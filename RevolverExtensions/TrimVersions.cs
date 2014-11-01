using Revolver.Core;
using Revolver.Core.Commands;
using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RevolverExtensions
{ 
    public class TrimVersions : ICommand
    {
        private const string RecursiveSwitch = "-r";
        private Context _context;
        private bool _recursive;

        public void Initialise(Context context, IFormatContext formatContext)
        {
            _context = context;
        }

        public CommandResult Run(string[] args)
        {
            ReadArgs(args);

            if (_context == null)
            {
                return new CommandResult(CommandStatus.Failure, "No Context");
            }

            if (_context.CurrentItem == null)
            {
                return new CommandResult(CommandStatus.Failure, "No Context Item");
            }

            var currentItem = _context.CurrentItem;
            if (currentItem.Versions.Count == 1 && !_recursive)
            {
                return new CommandResult(CommandStatus.Success, "Item only has one verison");
            }

            return BuildResponse(ProcessTrimVersionRequest(currentItem));
        }

        private CommandResult BuildResponse(string outputText)
        {
            return new CommandResult(CommandStatus.Success, outputText);
        }

        private void ReadArgs(IEnumerable<string> args)
        {
            _recursive = args.Any(x => x == RecursiveSwitch);
        }

        private string ProcessTrimVersionRequest(Item currentItem)
        {
            var outputText = TrimVersionsForItem(currentItem);
            if (_recursive)
            {
                foreach (Item childItem in currentItem.Children)
                {
                    outputText += ProcessTrimVersionRequest(childItem);
                }
            }

            return outputText;
        }

        private string TrimVersionsForItem(Item currentItem)
        {
            var outputTextLine = String.Empty;
            foreach (var language in currentItem.Languages)
            {
                var langItem = _context.CurrentDatabase.GetItem(currentItem.ID, language);
                if (langItem.Versions.Count <= 1)
                {
                    continue;
                }

                outputTextLine = String.Format("Trimmed:{0}, Lang:{1}, Versions:", currentItem.Paths.FullPath, langItem.Language.Name);
                foreach (var verItem in GetOldVersionsForLanguage(currentItem, langItem))
                {
                    outputTextLine += verItem.Version.Number.ToString(CultureInfo.InvariantCulture) + ",";
                    verItem.Versions.RemoveVersion();
                }
            }

            if (outputTextLine.EndsWith(","))
            {
                outputTextLine = outputTextLine.Substring(0, outputTextLine.Length - 1);
            }

            return String.IsNullOrEmpty(outputTextLine)
                       ? outputTextLine
                       : outputTextLine + "\r\n";
        }

        private static IEnumerable<Item> GetOldVersionsForLanguage(Item currentItem, Item langItem)
        {
            return langItem.Versions.GetVersions().Where(x => x.Version != currentItem.Versions.GetLatestVersion(langItem.Language).Version);
        }

        public string Description()
        {
            return "Trims all versions of an item apart from the latest one";
        }

        public HelpDetails Help()
        {
            var helpDetails = new HelpDetails
                {
                    Description = this.Description(), Usage = "<cmd> [-r]"
                };
            helpDetails.AddParameter("-r", "Optional. Recursive. Trim versions of all child items recursively.");
            helpDetails.AddExample("<cmd>");
            helpDetails.AddExample("<cmd> -r");
            return helpDetails;
        }
    }
}