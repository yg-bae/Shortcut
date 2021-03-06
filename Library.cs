﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using AutoUpdaterDotNET;

namespace Shortcut
{
    public partial class FrmMain
    {
        enum CmdEditType { ADD, EDIT };
        enum ValidPath
        {
            PATH_NONE = 0,
            PATH_VALID,
            PATH_INVALID
        };
        enum SearchDirections { TO_PARENT, TO_CHILD, TO_BOTH };

        #region ============================== Command Control ==============================
        public Command SetProperRunState(Command cmd)
        {
            if (cmd.Path == "") cmd.Run = false;
            return cmd;
        }

        private ValidPath ChkValidPath(string path)
        {
            if ((path == null) || (path == ""))
                return ValidPath.PATH_NONE;
            if (Directory.Exists(path) || File.Exists(path))
                return ValidPath.PATH_VALID;
            else
                return ValidPath.PATH_INVALID;
        }

        private void RunCmd(TreeNode node)
        {
            if (node.Tag != null)
            {
                Command cmd = new Command(node);

                if (cmd.Run == true)
                {
                    string path = cmd.GetAbsolutePath();
                    string arguments = cmd.GetAbsoluteArguments();
                    ValidPath validPath = ChkValidPath(path);
                    if (validPath == ValidPath.PATH_NONE)
                    {
                        // No run.
                    }
                    else if (validPath == ValidPath.PATH_VALID)
                    {
                        ProcessStartInfo processInfo = new ProcessStartInfo();
                        Process process = new Process();

                        processInfo.FileName = path;
                        processInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(path);
                        if (arguments != "")
                            processInfo.Arguments = arguments;
                        process.StartInfo = processInfo;
                        process.Start();
                        MinimizeToTray();
                    }
                    else
                    {
                        MessageBox.Show("Please check the \"Path\" or \"Arguments\" in the command.", "The File or Folder is NOT existed.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        node.SelectedImageKey = node.ImageKey = SelectIcon(path);
                    }
                }
            }
        }

        private static void SaveTree(TreeView tree, string filename)
        {
            using (Stream file = File.Open(filename, FileMode.Create))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(file, tree.Nodes.Cast<TreeNode>().ToList());
            }
        }

        private static void LoadTree(TreeView tree, string filename)
        {

            if (!File.Exists(filename))
                filename = "init_cfg.bin";

            using (Stream file = File.Open(filename, FileMode.Open))
            {
                BinaryFormatter bf = new BinaryFormatter();
                object obj = bf.Deserialize(file);

                TreeNode[] nodeList = (obj as IEnumerable<TreeNode>).ToArray();
                tree.Nodes.AddRange(nodeList);
            }
        }

        private void OpenDialog_NodeAdd(Point positionInTreeView)
        {
            TreeNode NodeOver = TreeView.SelectedNode;
            FrmInputDialog inputDialog;

            if (dragNdropPath != null)
            {
                Command cmdDragDrop = new Command(Path.GetFileNameWithoutExtension(dragNdropPath), true, dragNdropPath, null);
                inputDialog = new FrmInputDialog(cmdDragDrop, TreeView);
            }
            else
            {
                inputDialog = new FrmInputDialog(TreeView);
            }

            Command cmd = OpenCmdDialog(CmdEditType.ADD, ref inputDialog, NodeOver);
            if (cmd != null)
            {
                TreeNode newNode = cmd.GetTreeNode();

                if (NodeOver == null)
                {
                    TreeView.Nodes.Add(newNode);
                }
                else
                {
                    InsertCmd(TreeView, NodeOver, newNode, positionInTreeView.Y);
                    NodeOver.Expand();
                }

                newNode.SelectedImageKey = newNode.ImageKey = SelectIcon(cmd.GetAbsolutePath());
                SaveTree(TreeView, cfgFileName);
            }
            dragNdropPath = null;
        }

        private Command OpenCmdDialog(CmdEditType cmdEditType, ref FrmInputDialog inputDialog, TreeNode selectedNode)
        {
            while (inputDialog.ShowDialog() == DialogResult.OK)
            {
                Command cmd = inputDialog.GetCmdSet();
                if (ChkValidCmd(cmdEditType, selectedNode, cmd) == true)
                    return SetProperRunState(cmd);
            }
            return null;
        }

        private bool ChkValidCmd(CmdEditType cmdEditType, TreeNode selectedNode, Command cmd)
        {
            // Check redundant command
            TreeNodeCollection cmdGrp;
            if ((selectedNode == null)
                || ((cmdEditType == CmdEditType.EDIT) && (selectedNode.Level == 0)))
                cmdGrp = TreeView.Nodes;
            else if (cmdEditType == CmdEditType.ADD)
                cmdGrp = selectedNode.Nodes;
            else
                cmdGrp = selectedNode.Parent.Nodes;

            if (cmd.Name == "")
            {
                MessageBox.Show("커맨드 이름을 입력해주세요.");
                return false;
            }
            else if (cmdEditType == CmdEditType.ADD)
            {
                if (cmdGrp.ContainsKey(cmd.Name))
                {
                    MessageBox.Show("같은 이름의 커맨드 가 존재합니다.");
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                // 같은 이름의 node라도 그게 자기 자신인 경우는 제외
                TreeNode[] treeNodes = cmdGrp.Find(cmd.Name, false);
                if ((treeNodes.Length == 0) || (treeNodes[0] == selectedNode))
                {
                    return true;
                }
                else
                {
                    MessageBox.Show("같은 이름의 커맨드 가 존재합니다.");
                    return false;
                }
            }
        }

        private void InsertCmd(TreeView targetTree, TreeNode targetNode, TreeNode insertNode, int insertNodePositionY)
        {
            TreeNodeCollection targetParentCmd = (targetNode.Parent == null) ? targetTree.Nodes : targetNode.Parent.Nodes;

            switch (GetMovingCmdPositionOnTheTargetCmd(targetNode, insertNodePositionY))
            {
                case MovingCmdPosition.UPPER:
                    targetParentCmd.Insert(targetNode.Index, insertNode);
                    break;
                case MovingCmdPosition.OUTSIDE:
                case MovingCmdPosition.MIDDLE:
                    targetNode.Nodes.Add(insertNode);
                    break;
                case MovingCmdPosition.LOWER:
                    targetParentCmd.Insert(targetNode.Index + 1, insertNode);
                    break;
                default:
                    break;
            }
        }

        private void InsertCmd(TreeView targetTree, TreeNode targetCmd, TreeNode insertCmd, MovingCmdPosition insertNodePosition)
        {
            if(targetCmd == null)
            {
                TreeView.Nodes.Add(targetCmd);
            }
            else
            {
                TreeNodeCollection targetParentCmd = (targetCmd.Parent == null) ? targetTree.Nodes : targetCmd.Parent.Nodes;

                switch (insertNodePosition)
                {
                    case MovingCmdPosition.UPPER:
                        targetParentCmd.Insert(targetCmd.Index, insertCmd);
                        break;
                    case MovingCmdPosition.MIDDLE:
                    case MovingCmdPosition.OUTSIDE:
                        targetCmd.Nodes.Add(insertCmd);
                        break;
                    case MovingCmdPosition.LOWER:
                        targetParentCmd.Insert(targetCmd.Index + 1, insertCmd);
                        break;
                }
            }
        }

        private MovingCmdPosition GetMovingCmdPositionOnTheTargetCmd(TreeNode targetCmd, int cursorY)
        {
            int OffsetY = cursorY - targetCmd.Bounds.Top;

            if (OffsetY < (targetCmd.Bounds.Height / 3))            // 1/3 지점
                return MovingCmdPosition.UPPER;
            else if (OffsetY < (targetCmd.Bounds.Height * 2 / 3))   // 2/3 지점
                return MovingCmdPosition.MIDDLE;
            else if (OffsetY < targetCmd.Bounds.Height)             // 3/3 지점
                return MovingCmdPosition.LOWER;
            else
                return MovingCmdPosition.OUTSIDE;                    // Mouse Pointer와 선택된 노드가 너무 멀리있는 경우 -> Key board로 contextmenu를 open 한 경우
        }

        private void SearchCmd_Tree(TreeView targetTree, string name)
        {/*
            TreeNode[] tn = targetTree.Nodes[0].Nodes.Find(name, true);
            if (tn == null)
            {
                return;
            }
            else
            {
                for (int i = 0; i < tn.Length; i++)
                {
                    targetTree.SelectedNode = tn[i];
                    targetTree.SelectedNode.BackColor = Color.Yellow;
                }
            }
	    */
        }

        private TreeNode SearchCmd_ToChildNode(TreeNode targetNode, string name)
        {
            if(targetNode.Name != name)
            {
                foreach (TreeNode childNode in targetNode.Nodes)
                {
                    if (childNode.Name == name)
                        return childNode;
                    else
                        return SearchCmd_ToChildNode(childNode, name);
                }
	        	return null;
            }
	        else
            {
                return targetNode;
            }
        }

        private TreeNode SearchCmd_ToParentsNode(TreeNode targetNode, string name)
        {
            if (targetNode.Name != name)
            {
                if (targetNode.Parent != null)
                    return SearchCmd_ToParentsNode(targetNode.Parent, name);
                else
                    return null;
            }
            else
            {
                return targetNode;
            }
        }

        private void MoveCmdUpDown(TreeNode cmd, Keys dirKey)
        {
            int targetIdx = 0;
            TreeNode cloneNode = (TreeNode)cmd.Clone();

            switch (dirKey)
            {
                case Keys.Up:
                    targetIdx = (cmd.PrevNode == null) ? cmd.Index : cmd.PrevNode.Index;
                    break;
                case Keys.Down:
                    targetIdx = (cmd.NextNode == null) ? cmd.Index : cmd.NextNode.Index + 1;
                    break;
                default:
                    return;
            }

            if (cmd.Level == 0)
            {
                cmd.TreeView.Nodes.Insert(targetIdx, cloneNode);
                cmd.TreeView.SelectedNode = cloneNode;
                cmd.Remove();
            }
            else
            {
                cmd.Parent.Nodes.Insert(targetIdx, cloneNode);
                cmd.TreeView.SelectedNode = cloneNode;
                cmd.Remove();
            }
        }

        private void MoveCmdLeft(TreeNode cmd)
        {
            TreeNode cloneNode = (TreeNode)cmd.Clone();

            if (cmd.Level == 0)
            {
                ;
            }
            else if (cmd.Level == 1)
            {
                cmd.TreeView.Nodes.Insert(cmd.Parent.Index + 1, cloneNode);
                cmd.TreeView.SelectedNode = cloneNode;
                cmd.Remove();
            }
            else
            {
                cmd.Parent.Parent.Nodes.Insert(cmd.Parent.Index + 1, cloneNode);
                cmd.TreeView.SelectedNode = cloneNode;
                cmd.Remove();
            }
        }

        private void MoveCmdRight(TreeNode cmd)
        {
            TreeNode cloneNode = (TreeNode)cmd.Clone();

            if (cmd.PrevNode != null)
            {
                cmd.PrevNode.Nodes.Add(cloneNode);
                //cmd.PrevNode.Nodes.Insert(0, cloneNode);
                cmd.TreeView.SelectedNode = cloneNode;
                cmd.Remove();
            }
        }

        public TreeNode GotoNode_TopParent(TreeNode cmd)
        {
            while (cmd.Parent != null)
                cmd = cmd.Parent;
            return cmd;
        }

        public TreeNode GotoNode_PrevNodeOrParentPrevNode(TreeNode cmd)
        {
            if (cmd.Parent != null)
                return cmd.Parent;
            else
            {
                if (cmd.PrevNode != null)
                    return cmd.PrevNode;
                else
                    return cmd;
            }
        }

        public TreeNode GotoNode_LastNodeOrParentNextNode(TreeNode cmd)
        {
            if (cmd.Parent != null)
            {
                if (cmd.Parent.NextNode != null)
                    return cmd.Parent.NextNode;
                else
                    return GotoNode_LastNodeOrParentNextNode(cmd.Parent);
            }
            else
            {
                if (cmd.NextNode != null)
                    return cmd.NextNode;
                else
                    return cmd;
            }
        }

        private List<TreeNode> GetAllNodes(TreeView _self)
        {
            List<TreeNode> result = new List<TreeNode>();
            foreach (TreeNode child in _self.Nodes)
            {
                result.AddRange(GetAllNodes(child));
            }
            return result;
        }

        private List<TreeNode> GetAllNodes(TreeNode _self)
        {
            List<TreeNode> result = new List<TreeNode>();
            result.Add(_self);
            foreach (TreeNode child in _self.Nodes)
            {
                result.AddRange(GetAllNodes(child));
            }
            return result;
        }

        private void ShowContextMenu(TreeNode node)
        {
            Command cmd = new Command(node);
            string path = cmd.GetAbsolutePath();
            Point position = node.Bounds.Location;
            position.X = node.Bounds.Right;

            if (File.Exists(path))
            {
                ShellContextMenu ctxMnu = new ShellContextMenu();
                FileInfo[] arrFI = new FileInfo[1];
                arrFI[0] = new FileInfo(path);
                ctxMnu.ShowContextMenu(arrFI, TreeView.PointToScreen(position));
            }
            else if (Directory.Exists(path))
            {
                ShellContextMenu ctxMnu = new ShellContextMenu();
                DirectoryInfo[] arrFI = new DirectoryInfo[1];
                arrFI[0] = new DirectoryInfo(path);
                ctxMnu.ShowContextMenu(arrFI, TreeView.PointToScreen(position));
            }
        }
        #endregion  ============================== Command Control ==============================

        #region ============================== Tray Control ==============================
        private void MinimizeToTray()
        {
            if (options.GetOption_MinimizeToTrayAfterRun() == true)
                HideForm();
        }
        #endregion ============================== Tray Control ==============================

        #region ============================== Plance Holder Drawing ==============================
        private void DrawPlaceholder(TreeNode NodeOver, MovingCmdPosition placeHolderPosition)
        {
            Graphics g = TreeView.CreateGraphics();

            int NodeOverImageWidth = TreeView.ImageList.Images[NodeOver.ImageKey].Size.Width + 8;
            int LeftPos = NodeOver.Bounds.Left - NodeOverImageWidth;
            int RightPos = TreeView.Width - 4;
            int yPos = 0;
            if (placeHolderPosition == MovingCmdPosition.UPPER)
                yPos = NodeOver.Bounds.Top;
            else if (placeHolderPosition == MovingCmdPosition.LOWER)
                yPos = NodeOver.Bounds.Bottom;

            Point[] LeftTriangle = new Point[5]{
                                                   new Point(LeftPos, yPos - 4),
                                                   new Point(LeftPos, yPos + 4),
                                                   new Point(LeftPos + 4, yPos),
                                                   new Point(LeftPos + 4, yPos - 1),
                                                   new Point(LeftPos, yPos - 5)};

            Point[] RightTriangle = new Point[5]{
                                                    new Point(RightPos, yPos - 4),
                                                    new Point(RightPos, yPos + 4),
                                                    new Point(RightPos - 4, yPos),
                                                    new Point(RightPos - 4, yPos - 1),
                                                    new Point(RightPos, yPos - 5)};

            g.FillPolygon(System.Drawing.Brushes.White, LeftTriangle);
            g.FillPolygon(System.Drawing.Brushes.White, RightTriangle);
            g.DrawLine(new System.Drawing.Pen(Color.White, 2), new Point(LeftPos, yPos), new Point(RightPos, yPos));
        }

        private void DrawAddToFolderPlaceholder(TreeNode NodeOver)
        {
            Graphics g = TreeView.CreateGraphics();
            int RightPos = NodeOver.Bounds.Right + 6;

            Point[] RightTriangle = new Point[5]{
                                                    new Point(RightPos, NodeOver.Bounds.Y + (NodeOver.Bounds.Height / 2) + 4),
                                                    new Point(RightPos, NodeOver.Bounds.Y + (NodeOver.Bounds.Height / 2) + 4),
                                                    new Point(RightPos - 4, NodeOver.Bounds.Y + (NodeOver.Bounds.Height / 2)),
                                                    new Point(RightPos - 4, NodeOver.Bounds.Y + (NodeOver.Bounds.Height / 2) - 1),
                                                    new Point(RightPos, NodeOver.Bounds.Y + (NodeOver.Bounds.Height / 2) - 5)};

            g.FillPolygon(System.Drawing.Brushes.White, RightTriangle);
        }
        #endregion ============================== Plance Holder Drawing ==============================

        #region ============================== Icon Control ==============================
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, StringBuilder lpIconPath, out ushort lpiIcon);
        private string SelectIcon(string path)
        {
            if ( (path == "") || (path == null) )
            {
                return "Shortcut";
            }
            else if (System.IO.File.Exists(path))
            {
                string extension = Path.GetExtension(path).ToLower();
                if(     (extension != ".exe")
                    &&  (extension != ".ico")
                    &&  (iconList.Images.ContainsKey(extension) == true) )
                {
                    return extension;
                }
                    

                try
                {
                    StringBuilder strB = new StringBuilder(260); // Allocate MAX_PATH chars
                    strB.Append(path);

                    /* Icon을 얻는 방법 1 */
                    //ushort uicon;
                    //IntPtr handle = ExtractAssociatedIcon(IntPtr.Zero, strB, out uicon);
                    //Icon icon = Icon.FromHandle(handle);

                    /* Icon을 얻는 방법2 */
                    Icon icon = Icon.ExtractAssociatedIcon(path);

                    /* Icon을 얻는 방법3 */
                    //Icon icon = DefaultIcons.ExtractFromPath(path);

                    iconList.Images.Add(path, icon);
                    
                    return path;
                }
                catch (System.ArgumentException)  // CS0168
                {
                    return "Warning";
                }

            }
            else if (System.IO.Directory.Exists(path))
            {
                FileAttributes attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    return "Folder";
                else
                    return "Warning";
            }
            else
            {
                return "Warning";
            }
        }

        private void SetNodeIcon(TreeNode node)
        {
            Command cmd = new Command(node);
            node.SelectedImageKey = node.ImageKey = SelectIcon(cmd.GetAbsolutePath());
        }

        private void SetNodeIconRecursive(TreeNode parentNode)
        {
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            Command cmd = new Command(parentNode);
            //watch.Stop();
            //var elapsedTime_Instantiate = watch.ElapsedMilliseconds;

            SetNodeIcon(parentNode);

            foreach (TreeNode oSubNode in parentNode.Nodes)
            {
                SetNodeIconRecursive(oSubNode);
            }
        }
        #endregion ============================== Icon Control ==============================

    }

    public static class DefaultIcons
    {
        private static readonly Lazy<Icon> _lazyFolderIcon = new Lazy<Icon>(FetchIcon, true);

        public static Icon FolderLarge
        {
            get { return _lazyFolderIcon.Value; }
        }

        private static Icon FetchIcon()
        {
            var tmpDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
            var icon = ExtractFromPath(tmpDir);
            Directory.Delete(tmpDir);
            return icon;
        }

        private static Icon ExtractFromPath(string path)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            SHGetFileInfo(
                path,
                0, ref shinfo, (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_LARGEICON);
            return System.Drawing.Icon.FromHandle(shinfo.hIcon);
        }

        //Struct used by SHGetFileInfo function
        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;
        private const uint SHGFI_SMALLICON = 0x000000001;
    }
}
