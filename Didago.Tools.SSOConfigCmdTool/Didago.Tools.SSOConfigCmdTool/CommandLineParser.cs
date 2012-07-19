using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Didago.Tools.SSOConfigCmdTool
{
    /// <summary>
    /// Author  : Jean-Paul Smit - Didago IT Consultancy
    /// Date    : July 18, 2012
    ///
    /// This class describes the way the parameters look like.
    /// </summary>
    static class CommandLineParser
    {
        public static string Action { get; set; }
        public static string AppName { get; set; }
        public static string Key { get; set; }
        public static string File { get; set; }
        public static string DbServer { get; set; }
        public static string DbName { get; set; }

        public static string[] actions = { "import", "export", "delete", "detail", "list" };
        public static string[] cmdParams = { "-app:", "-key:", "-file:", "-dbsvr:", "-dbname:" };

        public static bool ParseArguments(string[] args)
        {
            // Check the number of arguemnts
            if (args.Length < 1 || args.Length > 6)
            {
                GetUsage();
                return false;
            }
            
            // Check if the action supplied is known
            CaseInsensitiveComparer ignoreCaseComparer = new CaseInsensitiveComparer();
            if (!actions.Contains(args[0], ignoreCaseComparer as IEqualityComparer<string>))
            {
                GetUsage();
                return false;
            }

            // Now parse the arguments, first get the action
            Action = args[0];
            DbServer = "localhost";
            DbName = "SSODB";

            // Next extract the list of allowed arguments from the parameters
            var paramList =  args.Where(c => cmdParams.Any(e =>c.StartsWith(e)));
            // But if the number of arguments parsed doesn't fit the arguments supplied, then there is an invalid argument
            if ((args.Length -1) != paramList.Count())
            {
                GetUsage();
                ConsoleColor current = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: invalid arguments!");
                Console.ForegroundColor = current;
                return false;
            }

            foreach (var p in paramList)
            {
                if (ParseValue(p, cmdParams[0]) != string.Empty) AppName = ParseValue(p, cmdParams[0]);
                if (ParseValue(p, cmdParams[1]) != string.Empty) Key = ParseValue(p, cmdParams[1]);
                if (ParseValue(p, cmdParams[2]) != string.Empty) File = ParseValue(p, cmdParams[2]);
                if (ParseValue(p, cmdParams[3]) != string.Empty) DbServer = ParseValue(p, cmdParams[3]);
                if (ParseValue(p, cmdParams[4]) != string.Empty) DbName = ParseValue(p, cmdParams[4]);
            }
            return true;
        }

        private static string ParseValue(string param, string cmdParam)
        {
            if (param.StartsWith(cmdParam))
            {
                return param.Substring(param.IndexOf(cmdParam) + cmdParam.Length);
            }
            return string.Empty;
        }

        /// <summary>
        /// Display the usage of the tool
        /// </summary>
        public static void GetUsage()
        {
            // this without using CommandLine.Text
            var usage = new StringBuilder();
            usage.Append(Environment.NewLine + "Didago IT Consultancy - SSOConfigCmdTool" + Environment.NewLine);
            usage.Append("By Jean-Paul Smit" + Environment.NewLine);
            usage.Append("Inspired by http://www.microsoft.com/en-us/download/details.aspx?id=14524" + Environment.NewLine);
            usage.Append("Commandline tool to import, export, delete, list and retrieve details of an SSO application" + Environment.NewLine + Environment.NewLine);
            usage.Append("***** Usage *****" + Environment.NewLine + Environment.NewLine);
            usage.Append("SSOConfigCmdTool <action> [-app:<sso appname>] [-key:<key>] [-file:<file>] [-dbsvr:<dbserver>] [-dbname:<dbname>]" + Environment.NewLine);
            usage.Append(Environment.NewLine);
            usage.Append("Available actions:" + Environment.NewLine);
            usage.Append("import -app:<sso appname> -key:<key> -file:<file> [-dbsvr:<dbserver> -dbname:<dbname>]" + Environment.NewLine);
            usage.Append(Environment.NewLine);
            usage.Append("export -app:<sso appname> -key:<key> -file:<file> [-dbsvr:<dbserver> -dbname:<dbname>]" + Environment.NewLine);
            usage.Append(Environment.NewLine);
            usage.Append("delete -app:<sso appname> [-dbsvr:<dbserver> -dbname:<dbname>]" + Environment.NewLine);
            usage.Append(Environment.NewLine);
            usage.Append("detail -app:<sso appname> [-dbsvr:<dbserver> -dbname:<dbname>]" + Environment.NewLine);
            usage.Append(Environment.NewLine);
            usage.Append("list [-dbsvr:<dbserver> -dbname:<dbname>]" + Environment.NewLine);
            usage.Append(Environment.NewLine);
            usage.Append("Explanation of parameters:" + Environment.NewLine);
            usage.Append(Environment.NewLine);
            usage.Append("sso appname =  name of the SSO application" + Environment.NewLine);
            usage.Append("key = encryption/decryption key" + Environment.NewLine);
            usage.Append("file = name of the file to import from or export to" + Environment.NewLine);
            usage.Append("dbserver = SSO database server name (optional, default is 'localhost')" + Environment.NewLine);
            usage.Append("dbname = SSO database name (optional, default is 'SSODB')" + Environment.NewLine);
            Console.WriteLine(usage.ToString());
        }
    }
}
