﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

namespace Shortcut
{
    public class Command
    {
        public enum Elements
        {
            NAME,
            RUN,
            PATH,
            ARGUMENTS
        }

        private const string Checked = "Checked";
        private const string Unchecked = "Unchecked";

        public string Name { get; set; } = null;
        public bool Run { get; set; } = false;
        public string Path { get; set; } = null;
        public string Arguments { get; set; } = null;

        #region Constructors 
        public Command(string name = null, bool run = false, string path = null, string arguments = null)
        {
            Name = name;
            Run = run;
            Path = path;
            Arguments = arguments;
        }

        public Command(TreeNode node)
        {
            if (node == null)
                return;
            else if (node.Tag == null)
            {
                Name = node.Name;
            }
            else
            {
                try
                {
                    Dictionary<string, string> cmd = (Dictionary<string, string>)node.Tag;
                    Name = cmd["Cmd"];
                    Run = (cmd["Run"] == "Checked");
                    Path = cmd["Path"];
                    Arguments = cmd["Arguments"];
                }
                catch
                {
                    // TreeView를 SaveTree할 때 Command Obj는 node의 tag로 save가 안되어서 Dictionary로 저장함
                    Dictionary<Elements, string> cmdDict = (Dictionary<Elements, string>)node.Tag;
                    Name = cmdDict[Elements.NAME];
                    Run = (cmdDict[Elements.RUN] == Checked);
                    Path = cmdDict[Elements.PATH];
                    Arguments = cmdDict[Elements.ARGUMENTS];
                }
            }
        }
        #endregion

        public string GetAbsolutePath(TreeNode targetNode)
        {
            return RemakeStringWithReplacingKeywords(Path, targetNode);
        }

        public string GetAbsoluteArguments(TreeNode targetNode)
        {
            return RemakeStringWithReplacingKeywords(Arguments, targetNode);
        }

        public TreeNode GetAsTreeNode()
        {
            TreeNode node = new TreeNode();
            node.Name = node.Text = Name;
            node.Tag = this;
            return node;
        }
        
        public Dictionary <Elements, string> ToDictionary() // TreeView를 SaveTree할 때 Command Obj는 node의 tag로 save가 안되어서 Dictionary로 저장함
        {
            Dictionary<Elements, string> cmdDict = new Dictionary<Elements, string>();
            cmdDict[Elements.NAME] = Name;
            cmdDict[Elements.RUN] = (Run) ? Checked : Unchecked;
            cmdDict[Elements.PATH] = Path;
            cmdDict[Elements.ARGUMENTS] = Arguments;
            return cmdDict;
        }

        public string RemakeStringWithReplacingKeywords(string originalString, TreeNode targetNode)
        {
            TreeNode parentNode = targetNode.Parent;

            if ( (originalString != null) && (parentNode != null) )
            {
                Command parentCmd = new Command(parentNode);
                if (originalString.Contains("#path#") && (parentCmd.Path != ""))
                {
                    return originalString.Replace("#path#", RemakeStringWithReplacingKeywords(parentCmd.Path, parentNode));
                }
            }
            return originalString;
        }
    }
}
