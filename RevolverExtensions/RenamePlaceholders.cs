using System.Linq;
using System.Xml.Linq;
using Revolver.Core;
using Revolver.Core.Commands;
using Sitecore.Data.Items;

namespace RevolverExtensions
{
    public class RenamePlaceholders : ICommand
    {
        private const string RecursiveSwitch = "-r";
        private const string RenderingElementName = "r";
        private const string RenderingsFieldName = "__renderings";
        private const string DeviceElementName = "d";
        private const string PlaceholderAttributeName = "ph";
        private Context _context;
        private bool _recursive;
        private string _oldPlaceHolderName;
        private string _newPlaceHolderName;

        public void Initialise(Context context, IFormatContext formatContext)
        {
            _context = context;
        }

        public CommandResult Run(string[] args)
        {
            ParseSwitchArgs(args);

            if (string.IsNullOrEmpty(_oldPlaceHolderName) || string.IsNullOrEmpty(_newPlaceHolderName))
            {
                return new CommandResult(CommandStatus.Failure, "Required arguements are missing, please use 'help rp' to view documentation");
            }

            if (_context == null)
            {
                return new CommandResult(CommandStatus.Failure, "No Context");
            }

            if (_context.CurrentItem == null)
            {
                return new CommandResult(CommandStatus.Failure, "No Context Item");
            }

            return BuildResponse(ProcessPlaceHolderRename(_context.CurrentItem));
        }

        private string ProcessPlaceHolderRename(Item currentItem)
        {
            var outputText = ReplacePlaceholderNameForItem(currentItem);
            if (_recursive)
            {
                foreach (Item childItem in currentItem.Children)
                {
                    outputText += ProcessPlaceHolderRename(childItem);
                }
            }

            return outputText;
        }

        private string ReplacePlaceholderNameForItem(Item item)
        {
            if (item != null && !string.IsNullOrEmpty(item[RenderingsFieldName]))
            {
                var renderingsElement = XElement.Parse(item[RenderingsFieldName]);
                var devices = renderingsElement.Elements(DeviceElementName);

                foreach (var device in devices)
                {
                    var renderings = device.Elements(RenderingElementName);
                    foreach (var rendering in renderings)
                    {
                        TrySetAttributeName(rendering);
                        TrySetAttributeNameWithNamespace(rendering);
                    }
                }

                using (new global::Sitecore.Data.Items.EditContext(item))
                {
                    item[RenderingsFieldName] = renderingsElement.ToString();
                }

                return item.Paths.Path + "\r\n";
            }

            return string.Empty;
        }

        private void TrySetAttributeNameWithNamespace(XElement rendering)
        {
            XNamespace ns = "s";
            var itemPlaceHolderAttribute = rendering.Attributes().FirstOrDefault(x => x.Name == ns + PlaceholderAttributeName);
            if (itemPlaceHolderAttribute != null && itemPlaceHolderAttribute.Value.Contains(_oldPlaceHolderName))
            {
                var newValue = itemPlaceHolderAttribute.Value.Replace(_oldPlaceHolderName, _newPlaceHolderName);
                itemPlaceHolderAttribute.SetValue(newValue);
            }
        }

        private void TrySetAttributeName(XElement rendering)
        {
            var placeHolderAttribute = rendering.Attributes().FirstOrDefault(x => x.Name == PlaceholderAttributeName);
            if (placeHolderAttribute != null && placeHolderAttribute.Value.Contains(_oldPlaceHolderName))
            {
                var newValue = placeHolderAttribute.Value.Replace(_oldPlaceHolderName, _newPlaceHolderName);
                placeHolderAttribute.SetValue(newValue);
            }
        }

        private CommandResult BuildResponse(string outputText)
        {
            return new CommandResult(CommandStatus.Success, outputText);
        }

        private void ParseSwitchArgs(string[] args)
        {
            _recursive = args.Any(x => x == RecursiveSwitch);

            if (args.Length >= 2)
            {
                _oldPlaceHolderName = args[0];
                _newPlaceHolderName = args[1];
            }
        }

        public string Description()
        {
            return "Replaces all instances of a placeholder name with new placeholder name";
        }

        public HelpDetails Help()
        {
            var helpDetails = new HelpDetails
            {
                Description = Description(),
                Usage = "<cmd> oldName newName [-r]"
            };
            helpDetails.AddParameter("oldName", "The existing name of the placeholder to be changed.");
            helpDetails.AddParameter("newName", "The new name for the placeholder to be used instead.");
            helpDetails.AddParameter("-r", "Optional. Recursive. Perform placeholder replacement on all child items recursively.");
            helpDetails.AddExample("<cmd>");
            helpDetails.AddExample("<cmd> placeholder-name placeholder-name -r");
            return helpDetails;
        }
    }

}
