using System.IO;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using AutoUsing.Utils;

namespace AutoUsing.Lsp
{
    class CompletionProvider : ICompletionHandler
    {
        // private CompletionCapability _capability;
        


        private readonly ILanguageServer LanguageServer;
        // private readonly FileManager _bufferManager;
        private readonly DocumentSelector _documentSelector = new DocumentSelector(
                new DocumentFilter()
                {
                    Pattern = "**/*.cs"
                }
            );
        // const int maxCompletionAmount = 100;

        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions
            {
                DocumentSelector = _documentSelector,
                ResolveProvider = false,
                TriggerCharacters= new Container<string>(".")
            };


        }

        public CompletionProvider(ILanguageServer server)
        {
            LanguageServer = server;
            // Util.Log("This got called!");
            // _bufferManager = bufferManager;
        }

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            
            // Util.Log("Handling completion request.");
            // return new CompletionList(new CompletionItem{Label ="new version op"});
            // var result = await CompletionInstance.ProvideCompletionItems(request, server, _bufferManager);
             var result = await CompletionInstance.ProvideCompletionItems(request, Server.Instance,LanguageServer);
            return result;
        }

        public void SetCapability(CompletionCapability capability)
        {
            // _capability = capability;
            // capability.
        }

        // /// <summary>
        // /// Get the list of completions that are commonly used by the user and are therefore stored in the system.
        // /// </summary>
        // public static IEnumerable<StoredCompletion> getStoredCompletions(vscode.ExtensionContext context)
        // {
        //     var completions = context.globalState.get<StoredCompletion[]>(COMPLETION_STORAGE);

        //     if(completions == null) return new List<StoredCompletion>();
        //     return completions;
        // }

        // const string COMPLETION_STORAGE = "commonwords";
    }

    public class StoredCompletion
    {
        public string Label { get; set; }
        public string Namespace { get; set; }
    }



}








// public constructor(private extensionContext: vscode.ExtensionContext, private server: AutoUsingServer) { }
// public async provideCompletionItems(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken,
//     context: vscode.CompletionContext): Promise<vscode.CompletionList> {

// let result = await provideCompletionItems(document, position, token, context, this.extensionContext, this.server);