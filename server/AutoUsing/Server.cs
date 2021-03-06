using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AutoUsing.Analysis;
using AutoUsing.Analysis.Cache;
using AutoUsing.Analysis.DataTypes;
using AutoUsing.Utils;
using AutoUsing.Lsp;
using Newtonsoft.Json;

namespace AutoUsing
{

    public class SetupWorkspaceRequest
    {
        public List<string> Projects { get; set; }
        public string WorkspaceStorageDir { get; set; }
        public string GlobalStorageDir { get; set; }
        public string ExtensionDir { get; set; }

    }

    /// <summary>
    /// Specifies the logic for every request
    /// </summary>
    public class Server
    {
        public static Server Instance = new Server();
        // public IOProxy Proxy = new IOProxy();
        private readonly List<Project> Projects = new List<Project>();
        private string GlobalStorageDir;


        /// <summary>
        /// Returns the list of types in the .NET base class library + the types in the libraries of the
        /// requested projected + the types in the requested project, as long as they contain the
        /// "WordToComplete" field in the request
        /// </summary>
        public List<TypeCompletion> GetAllTypes(string projectName, string wordToComplete)
        {
            var project = FindProject(projectName);

            var typeInfo = GlobalCache.Caches.Types.GetCache().Concat(project.Caches.Types.GetCache()).ToList();
            //TODO: remove
            typeInfo.Sort((e1,e2) => e1.Name.CompareTo(e2.Name));
            var filtered = typeInfo.Where(t => t.Name.Contains("Fi"));

            var refinedTypeData = FilterUnnecessaryData(CompletionCaches.ToCompletionFormat(typeInfo),
                wordToComplete, (type) => type.Name);

            return refinedTypeData;
        }

        /// <summary>
        /// Removes completion data that is unneeded because the user has already filered them.
        /// </summary>
        /// <param name="dataList">Completion data</param>
        /// <param name="wordToComplete">The characters the user has type so far</param>
        /// <param name="identifierOfElements">A function that returns what is shown as the actual completion for a specific data element</param>
        /// <typeparam name="T">The type of completion data</typeparam>
        /// <returns>The data list filitered</returns>
        private static List<T> FilterUnnecessaryData<T>(List<T> dataList, string wordToComplete,
            Func<T, string> identifierOfElements)
        {
            return dataList.Where(element => identifierOfElements(element).ToLower().StartsWith(wordToComplete.ToLower()))
                .ToList();
        }


        public List<ExtensionClass> GetAllExtensionMethods(string projectName, string wordToComplete)
        {
            var project = FindProject(projectName);

            var extensionInfo = GlobalCache.Caches.Extensions.GetCache().Concat(project.Caches.Extensions.GetCache()).ToList();
            var refinedExtensionData = CompletionCaches.ToCompletionFormat(extensionInfo);

            refinedExtensionData = refinedExtensionData
                .Select(extendedClass => new ExtensionClass(extendedClass.ExtendedClass,
                 FilterUnnecessaryData(extendedClass.ExtensionMethods, wordToComplete, (extensionMethod) => extensionMethod.Name))
                 )
                .Where(extendedClass => extendedClass.ExtensionMethods.Count > 0).ToList();

            return refinedExtensionData;
        }

        public List<Hierarchies> GetAllHierarchies(string projectName)
        {
            var project = FindProject(projectName);

            var hierarchyInfo = GlobalCache.Caches.Hierachies.GetCache().Concat(project.Caches.Hierachies.GetCache()).ToList();
            return CompletionCaches.ToCompletionFormat(hierarchyInfo);
        }

        /// <summary>
        /// Retrieves the Project object out of the list of projects that were added. 
        /// </summary>
        /// <param name="projectName">The file name of the project without the .csproj extension</param>
        /// <returns>The project if the project was added before, and null otherwise.</returns>
        /// <exception cref = "ServerException">Throws ServerException if the project was not found or the project name is empty.</throws>
        private Project FindProject(string projectName)
        {
            if (projectName.IsNullOrEmpty())
            {
                throw new ServerException("The project name must not be empty.");
            }

            // Using C# 7.2 `is expression` to check for null, and assign variable
            if (Projects.Find(o => o.Name == projectName) is Project project)
            {
                return project;
            }
            else
            {
                throw new ServerException(
                       $"\nRequested project {projectName} is not in this list: {{{String.Join(", ", Projects.Select(proj => proj.Name))}}}");
            }

        

        }




        /// <summary>
        /// Adds .NET projects for the server to watch over and collect assembly info about.
        /// </summary>
        public void SetupWorkspace(SetupWorkspaceRequest req)
        {
            GlobalStorageDir = req.GlobalStorageDir;
            GlobalCache.SetupGlobalCache(GlobalStorageDir);

            if (req.Projects.Any(path => !File.Exists(path)))
            {
                throw new ServerException(Errors.NonExistentProject);
            }

            const string csproj = ".csproj";

            if (req.Projects.Any(path => Path.GetExtension(path) != csproj))
            {
                throw new ServerException(Errors.NonExistentProject);
            }

            Projects.AddRange(req.Projects.Select(path => new Project(path, req.WorkspaceStorageDir, watch: true)));
        }

        /// <summary>
        /// Returns completions that have been chosen by the user before. The client side is the one who stores these completions.
        /// </summary>
        public IEnumerable<StoredCompletion> GetCommonCompletions()
        {
            var location = CommonCompletionLocation();
            if (!Directory.Exists(CommonCompletionDirectory()))
            {
                Directory.CreateDirectory(CommonCompletionDirectory());
                return InvalidStorage(location);
            }
            else if (!File.Exists(location))
            {
                return InvalidStorage(location);
            }

            var text = File.ReadAllText(location);
            if (text == "")
            {
                return InvalidStorage(location);
            }

            try
            {
                var completions = JSON.Parse<List<StoredCompletion>>(text);
                return completions;
            }
            // Someone fucked up the memory file for the common completions
            catch (JsonSerializationException)
            {
                return InvalidStorage(location);
            }




        }

        private IEnumerable<StoredCompletion> InvalidStorage(string location)
        {
            File.WriteAllText(location, "[]");
            return new List<StoredCompletion>();
        }




        private string CommonCompletionLocation() => Path.Join(CommonCompletionDirectory(), "commonCompletions.json");
        private string CommonCompletionDirectory() => Path.Join(GlobalStorageDir, "completions");

    }
}