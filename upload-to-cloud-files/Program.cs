using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using net.openstack.Providers.Rackspace;
using net.openstack.Core.Domain;
using CommandLine;
using CommandLine.Text;
using System.IO;
using net.openstack.Core.Exceptions.Response;

namespace upload_to_cloud_files
{
    class Options
    {
        [Option('f', "file", Required = true, HelpText = "Path to local file to be uploaded.")]
        public string LocalFilePath { get; set; }

        [Option('r', "region", DefaultValue = "IAD", HelpText = "Rackspace Region.")]
        public string Region { get; set; }

        [Option('c', "container", Required = true, HelpText = "Rackspace Cloud Files Container.")]
        public string Container { get; set; }

        [Option('n', "name", Required = true, HelpText = "Remote file name (can include / as path separator).")]
        public string RemoteObjectName { get; set; }

        [Option('u', "username", Required = true, HelpText = "Rackspace username.")]
        public string Username { get; set; }

        [Option('a', "apikey", Required = true, HelpText = "Rackspace API Key.")]
        public string ApiKey { get; set; }

        [Option('i', "internalUrl", DefaultValue = false, HelpText = "Set to true if running from within the same Rackspace Region (uses the internal API, saves bandwidth costs on the public interface)")]
        public bool UseInternalUrl { get; set; }

        [Option('o', "overwrite", DefaultValue = false, HelpText = "Set to true to overwrite an existing file")]
        public bool Overwrite { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, options)) {
                return 1;
            }
            if (!File.Exists(options.LocalFilePath)) {
                Console.WriteLine("ERROR: local file not found");
                return 2;
            }

            CloudIdentity cloudIdentity = new CloudIdentity() {
                Username = options.Username,
                APIKey = options.ApiKey
            };
            CloudIdentityProvider cloudIdentityProvider = new CloudIdentityProvider(cloudIdentity);
            Console.WriteLine("Uploading [{0}] -> [{1}:{2}/{3}]", options.LocalFilePath, options.Region, options.Container, options.RemoteObjectName);
            try {
                // TODO: check container existance and return a specific error code in case of failure
                CloudFilesProvider cfp = new CloudFilesProvider(cloudIdentity);
                if (!options.Overwrite) {
                    try
                    {
                        cfp.GetObjectMetaData(
                            container: options.Container,
                            region: options.Region,
                            useInternalUrl: options.UseInternalUrl,
                            objectName: options.RemoteObjectName
                        );
                        Console.WriteLine("ERROR: Remote file already exists and overwrite is set to false");
                        return 3;
                    }
                    catch (ItemNotFoundException /*e*/) {
                        // All is good: remote file does not exist
                        // TODO: figure out if we can do this without involving exceptions...
                    }
                    catch (Exception e) {
                        Console.WriteLine("ERROR: failed checking if file already exists. Reason: {0}", e.Message, e);
                        return 4;
                    }
                }

                cfp.CreateObjectFromFile(
                    container: options.Container,
                    region: options.Region,
                    useInternalUrl: options.UseInternalUrl,
                    objectName: options.RemoteObjectName,
                    filePath: options.LocalFilePath
                );
                Console.WriteLine("Done");
                return 0;
            }
            catch (Exception e) {
                Console.WriteLine("ERROR: Upload failed with message: {0}", e.Message);
                //Console.WriteLine("{0}", e);
                return 4;
            }
            
        }
    }
}
