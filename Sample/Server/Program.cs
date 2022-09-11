using LspTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).Wait();

        static async Task MainAsync(string[] args)
        {
            Stream stdin = Console.OpenStandardInput();
            Stream stdout = Console.OpenStandardOutput();
            stdin = new Tee(stdin, new Dup("editor"), Tee.StreamOwnership.OwnNone);
            stdout = new Tee(stdout, new Dup("server"), Tee.StreamOwnership.OwnNone);
            var languageServer = new LSPServer(stdout, stdin);
            await Task.Delay(-1);
        }
    }

    class LSPServer : INotifyPropertyChanged, IDisposable
    {
        readonly JsonRpc rpc;
        readonly ManualResetEvent disconnectEvent = new ManualResetEvent(false);
        Dictionary<string, DiagnosticSeverity> diagnostics;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Disconnected;
        bool isDisposed;

        public LSPServer(Stream sender, Stream reader)
        {
            rpc = JsonRpc.Attach(sender, reader, this);
            rpc.Disconnected += OnRpcDisconnected;
        }
        void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e) => Exit();
        void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            if (disposing)
            {
                // free managed resources
                disconnectEvent.Dispose();
            }
            isDisposed = true;
        }
        public void WaitForExit()
        {
            disconnectEvent.WaitOne();
        }
        ~LSPServer()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        public void Exit()
        {
            disconnectEvent.Set();
            Disconnected?.Invoke(this, new EventArgs());
            Environment.Exit(0);
        }

        static readonly object _object = new object();
        readonly bool trace = true; // not effected by setting??

        void traceLine(string line)
        {
            if (trace) Console.Error.WriteLine(line);
        }

        [JsonRpcMethod(Methods.InitializeName)]
        public object Initialize(JToken arg)
        {
            lock (_object)
            {
                traceLine("<-- Initialize");
                traceLine(arg.ToString());

                //var init_params = arg.ToObject<InitializeParams>();

                var result = new InitializeResult() { Capabilities = GetCapabilities() };
                traceLine("--> " + JsonConvert.SerializeObject(result));

                return result;
            }
        }

        ServerCapabilities GetCapabilities()
        {
            return new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.Incremental,
                    Save = new SaveOptions
                    {
                        IncludeText = true
                    }
                },

                CompletionProvider = null,

                HoverProvider = true,

                SignatureHelpProvider = null,

                DefinitionProvider = true,

                TypeDefinitionProvider = false,

                ImplementationProvider = false,

                ReferencesProvider = true,

                DocumentHighlightProvider = true,

                DocumentSymbolProvider = true,

                CodeLensProvider = null,

                DocumentLinkProvider = null,

                DocumentFormattingProvider = true,

                DocumentRangeFormattingProvider = false,

                RenameProvider = true,

                FoldingRangeProvider = new SumType<bool, FoldingRangeOptions, FoldingRangeRegistrationOptions>(false),

                ExecuteCommandProvider = null,

                WorkspaceSymbolProvider = false,

                SemanticTokensProvider = new SemanticTokensOptions()
                {
                    Full = true,
                    Range = false,
                    Legend = new SemanticTokensLegend()
                    {
                        tokenTypes = new string[] {
                                "class",
                                "variable",
                                "enum",
                                "comment",
                                "string",
                                "keyword",
                            },
                        tokenModifiers = new string[] {
                                "declaration",
                                "documentation",
                            }
                    }
                },
            };
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public void InitializedName(JToken arg)
        {
            lock (_object)
            {
                try
                {
                    traceLine("<-- Initialized");
                    traceLine("arg.ToString()");
                }
                catch (Exception)
                { }
            }
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public JToken ShutdownName()
        {
            lock (_object)
            {
                try
                {
                    traceLine("<-- Shutdown");
                }
                catch (Exception)
                { }
                return null;
            }
        }

        [JsonRpcMethod(Methods.ExitName)]
        public void ExitName()
        {
            lock (_object)
            {
                try
                {
                    traceLine("<-- Exit");
                    Exit();
                }
                catch (Exception)
                { }
            }
        }
    }
}
