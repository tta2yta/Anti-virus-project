#region using statements
using System;
using System.Media;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security .Cryptography ;
using Microsoft.VisualBasic;
using System.Resources;
using System.Management;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Ionic.Zip;
using Microsoft.Win32;
using System.IO.Compression;
using Antivirus;
#endregion
namespace Antivirus
{
    public partial class frmAntivirus : Form
    {
        #region member variables' declaration
        List<string> qrtn = new List<string>();
        Boolean scanonoff;
        Boolean exitfrm;
        Boolean dbupdate;
        string keyvalue;
        Boolean onaccess;
        Boolean  checkforsimilarity = true;
        Boolean exit;
        ListBox list1 = new ListBox();
        ListBox list2 = new ListBox();
        ListBox list3 = new ListBox();
        List<string> viruslist = new List<string>();
        TextBox scanbox = new TextBox();
        string s = "";
        int totfiles;
        MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
        string[] dirs;
        // device state change
        private const int WM_DEVICECHANGE = 0x0219;

        // logical volume(A disk has been inserted, such a usb key or external HDD)
        private const int DBT_DEVTYP_VOLUME = 0x00000002;

        // detected a new device
        private const int DBT_DEVICEARRIVAL = 0x8000;

        // preparing to remove
        private const int DBT_DEVICEQUERYREMOVE = 0x8001;
       // DEV_BROADCAST_VOLUME volume = (DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(message.LParam, typeof(DEV_BROADCAST_VOLUME));
        
        // removed
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        #endregion
        #region device recognition
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);

            if ((message.Msg != WM_DEVICECHANGE) || (message.LParam == IntPtr.Zero))
                return;
            string[] drive=new string[1];
            switch (message.WParam.ToInt32())
            {
                // New device inserted...
                case DBT_DEVICEARRIVAL:
                    if (onaccess == false)
                    {
                        break;
                    }
                    clear();
                    this.Visible = true;
                    treeView1.Nodes.Clear();
                    drives();
                    
                    foreach( string dd in Environment.GetLogicalDrives())
                    {
                        DriveInfo d=new DriveInfo(dd);
                        if(d.IsReady)
                         drive[0] = d.RootDirectory.FullName;
                    }
                    notifyIcon1.BalloonTipText = "On Acess Scan Started \n Scanning Drive::" +drive[0];
                    notifyIcon1.ShowBalloonTip(100000);
                    notifyIcon1.BalloonTipTitle = "Argefom";
                     tabControl2.SelectTab(1);
                    
                //customfullscan();
                try
                {
                    recurse(drive[0]);
                }

                catch (Exception )
                {
                }

                tabControl2.SelectTab(1);
                    
                timer1.Start();
                    break;

                // Device Removed.
                case DBT_DEVICEREMOVECOMPLETE:
                    treeView1.Nodes.Clear();
                    drives();

                    break;
            }

        }
        #endregion
        #region form constructor
        public frmAntivirus()
        {
            InitializeComponent();
        }
        #endregion
        #region form_load event
        private void Form1_Load(object sender, EventArgs e)
        {
            checkfordboutdate();
            if (dbupdate == false)
            {
                lblupdate.ForeColor = Color.Red;
                lblupdate.Text = "Virus Sgnature DataBase Is OutDated Please Update It??";
            }
            else
            {
                lblupdate.ForeColor = Color.Blue;
                lblupdate.Text = "Virus Sgnature DataBase Is Updated!!";
            }
            onaccess = true;
            realtime.Text = "System Enabled";
            listView2.Items.Clear();
            listView1.Items.Clear();
                FileStream ff = new FileStream("viruslist.txt", FileMode.Open, FileAccess.Read);
                StreamReader rdr = new StreamReader(ff);
                string str = rdr.ReadLine();
                while (str != null)
                {
                    viruslist.Add(str.Trim());
                    str = rdr.ReadLine();
                }
                scanbox.Text = rdr.ReadToEnd();
                rdr.Close();                   
                ff.Close();
                scanonoff = false;
                drives();
                clear();
                radioButton1.Checked = true;
        }
        #endregion
        #region drives recognition function
        public void drives()
        {
            string[] drives = Environment.GetLogicalDrives();
            foreach (string dr in drives)
            {
                DriveInfo dd = new DriveInfo(dr);

                if (dd.IsReady)
                {
                    TreeNode node = new TreeNode(dr);
                    node.Tag = dr;
                    node.ImageIndex = 0; // drive icon
                    node.Tag = dr;
                    treeView1.Nodes.Add(node);
                    node.Nodes.Add(new TreeNode("?"));
                }
            }
            treeView1.BeforeExpand += new TreeViewCancelEventHandler(treeView1_BeforeExpand);
        }
        #endregion
        #region treeView1_BeforeExpand event calls RecursiveDirWalk function
        void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if ((e.Node.Nodes.Count == 1) && (e.Node.Nodes[0].Text == "?"))
            {
                RecursiveDirWalk(e.Node);
            }
        }
        #endregion
        #region RecursiveDirWalk function accepts and returns TreeNode
        private TreeNode RecursiveDirWalk(TreeNode node)
        {
            string path = (string)node.Tag;
            node.Nodes.Clear();
            try
            {
                dirs = System.IO.Directory.GetDirectories(path);
            }
            catch (UnauthorizedAccessException )
            {
            }
            for (int t = 0; t < dirs.Length; t++)
            {
                TreeNode n = new TreeNode(dirs[t].Substring(dirs[t].LastIndexOf('\\') + 1));
                n.ImageIndex = 1; 
                n.Tag = dirs[t];
                node.Nodes.Add(n);
                n.Nodes.Add(new TreeNode("?"));
            }

            return node;
        }
        #endregion
        #region RecurseFolders recursive function to add nodes to tree
        public void RecurseFolders(string path, TreeNode node)
        {
            var dir = new DirectoryInfo(path);
            var drive = new DriveInfo(path);

            node.Text = dir.Name;

            try
            {
               

                    foreach (string subdir in Directory.GetDirectories(dir.Name))
                    {
                        
                            var childnode = new TreeNode();
                            childnode.ImageIndex = 1;
                            node.Nodes.Add(childnode);
                            RecurseFolders(subdir, childnode);
                        }
                    }
            

            catch (UnauthorizedAccessException )
            {
                node.Nodes.Add("Access denied");
            }
            catch (DirectoryNotFoundException )
            {
                node.Nodes.Add("Access");
            }
            catch (IOException )
            {
            }
            
        }
        #endregion
        #region Function to start Task Manager
        private void cmdtask_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("taskmgr.exe");
        }
        #endregion
        #region Function to traverse the directories recursively to add them to the tree
        public void recurse(string scan)
        {
            if (list1.Items.Count >= 7000)
            {
                return;
            }
            try
            {
                foreach (string s in Directory.GetFiles(scan))
                {
                    FileInfo f = new FileInfo(s);
                    if (f.Extension == ".exe" || f.Extension == ".pif" || f.Extension == ".inf" || f.Extension == ".dll" || f.Extension == ".ini" || f.Extension == ".bat" || f.Extension == ".com" || f.Extension == ".lnk")
                    {
                        list1.Items.Add(s);
                    }
                }
                foreach (string str in Directory.GetDirectories(scan))
                {
                    recurse(str);
                }
            }
            catch (Exception )
            {
            }
            totfiles = list1.Items.Count; 
        }
        #endregion
        #region button Scan click event
        private void cmdscan_Click(object sender, EventArgs e)
        {
            menuscan_Click(menuscan, null); 
        }
        #endregion
        #region startscanning function which initializes the tree nodes and calls the timer
        public void startscanning()
        {
            
            listView1.Items.Clear();
            list1.Items.Clear();
            list2.Items.Clear();
            if (radcustom.Checked == true && treeView1.SelectedNode != null)
            {
                tabControl2.SelectTab(1);
                string path = treeView1.SelectedNode.FullPath;
                recurse(path);
                timer1.Start();
            }
            else if (radfull.Checked == true)
            {
                tabControl2.SelectTab(1);

                try
                {
                    foreach (string d in Environment.GetLogicalDrives())
                    {

                        DriveInfo dd = new DriveInfo(d);
                        if (dd.IsReady)
                        {
                            recurse(d);

                        }
                    }
                }


                catch (Exception )
                {

                }

               timer1.Start();
            }
            else
            {
                if (radioButton1.Checked == true)
                {
                    MessageBox.Show("Please select a drive or folder", "Argefom");
                }
                else if(radioButton2.Checked == true)
                {
                    MessageBox.Show("ብክብረትካ ፎልደር ምረጽ", "ኣርገፎም");
                }
            }
        }
        #endregion
        #region tab control result selected event
        private void cmdresult_Click(object sender, EventArgs e)
        {
            lbltotr.Text = "Total Files:" + listView1.Items.Count.ToString();
            tabControl2.SelectTab(2);
        }
        #endregion
        #region timer1_Tick event calls the scanning function
        private void timer1_Tick(object sender, EventArgs e)
         {
             timer1.Stop();
             listView2.Items.Clear();
             scanonoff = true;
             if (radioButton1.Checked == true)
             {
                 this.Text = "Scanning...";
             }
             else if (radioButton2.Checked == true)
             {
                 this.Text = "ስካኒን ይካየድ ኣሎ...";
             }
          
             progressBar1.Maximum = totfiles;
             if ((progressBar1.Value != progressBar1.Maximum))
             {
                 try
                 {
                     list1.SelectedIndex = list1.SelectedIndex + 1;
                     lblfile.Text = Convert.ToString(list1.SelectedItem);
                     
                 }

                 catch (Exception)
                 {
                 }
                 try
                 {
                     progressBar1.Increment(1);
                     label2.Text = Convert.ToString(totfiles);
                     label3.Text = Convert.ToString(progressBar1.Value);
                     float x = (progressBar1.Value * 100) / progressBar1.Maximum;
                     label7.Text = Convert.ToString(x) + "%";
                     string file = Convert.ToString(list1.SelectedItem);
                     FileInfo fdd = new FileInfo(@file);
                     //string sf = fdd.Extension;
                    // if (sf == ".zip")
                     //{
                        // recurse1(file);  
                        // list1.SelectedIndex = list1.SelectedIndex + 1;
                         //lblfile.Text = Convert.ToString(list1.SelectedItem);
                        // file = Convert.ToString(list1.SelectedItem);
                     //}
                     string pp = generatehash(file);
                     label8.Text = "" + list2.Items.Count;
                     foreach (string str in viruslist)
                     {
                         string vname;
                         string sig = str.Trim();
                         if (sig.Substring(sig.IndexOf("=") + 1).Equals(pp.Trim()))
                         {
                             System.Diagnostics.Process[] p = System.Diagnostics.Process.GetProcesses();
                             for (int ii = 0; ii < p.Length; ii++)
                             {
                                 if (p[ii].ProcessName == fdd.Name)
                                 {
                                     p[ii].Kill();
                                     p[ii].WaitForExit();
                                     FileInfo fiii = new System.IO.FileInfo(@"C:\WINDOWS\system32\" + fdd.Name);
                                     fiii.Attributes = System.IO.FileAttributes.Normal;
                                     //fiii.Delete();
                                 }
                             }
                             checkforsimilarity = false;
                             list2.Items.Add(sig.Substring(0, sig.IndexOf("=") - 1));
                             label8.Text = "" + list2.Items.Count;
                             //System.Media.SystemSounds.Asterisk.Play();
                             ListViewItem itm = new ListViewItem();
                             itm.Text = fdd.Name;
                             string hh = sig.Substring(0, sig.IndexOf("="));
                             itm.SubItems.Add(hh);
                             itm.SubItems.Add(Convert.ToString(list1.SelectedItem));
                             itm.SubItems.Add("Deleted");
                             itm.SubItems.Add(DateTime.Now.ToString());
                             listView1.Items.Add(itm);
                             vname = list1.SelectedItem.ToString();
                             string s = Environment.CurrentDirectory;
                             string p2 = s.Replace("'/'", "'\'");
                             string p3 = p2 + @"\Quarantine\";
                             p2 = p2 + @"\Quarantine\" + fdd.Name;
                             try
                             {
                                 
                                     //EncryptFile(@vname, @p2, "?>?~y>?");
                                     //qrtn.Add(hh + "=" + list1.SelectedItem.ToString());
                                     File.SetAttributes(@vname, FileAttributes.Normal);
                                     File.Delete(@vname);  
                             }

                             catch (Exception ex)
                             {
                                // MessageBox.Show(ex.ToString());
                                 //timer1.Start();
                             }
                         }
                     }

                     if (checkforsimilarity == true)
                     {
                         //MessageBox.Show(checkforsimilarity.ToString());
                         similarityfile(list1.SelectedItem.ToString());
                         timer1.Start();
                     }

                 }
                 catch (IOException ex)
                 {
                     //timer1.Start();
                 }

                 catch (Exception ex)
                 {
                     //timer1.Start();
                 }
                 timer1.Start();
             }
                
             else
             {
                 timer1.Stop();
                 this.Text = "Antivirus";
                 FileStream hh = new FileStream("quarantineinfo.txt",FileMode.Append ,FileAccess .Write ,FileShare .None  );
                 StreamWriter sw = new StreamWriter(hh);
                 foreach (string str in qrtn)
                 {
                     sw.WriteLine(str);
                 }
                 qrtn.Clear();
                 sw.Dispose();
                 sw.Close();
                 if (radioButton1.Checked == true)
                 {
                     MessageBox.Show("Finished Scanning", "Argefom");
                 }
                 else if (radioButton2.Checked == true)
                 {
                     MessageBox.Show("ስካኒን ተፈጺሙ", "ኣርገፎም");
                 }
                 scanonoff = false;
                 if (list2.Items.Count == 0)
                 {
                     tabControl2.SelectTab(0);
                 }
                 else
                 {
                     lbltotr.Text = "Total Files:" + listView1.Items.Count;
                     tabControl2.SelectTab(2);
                 }

                 clear();

             }     
         }
        #endregion
        #region To encrypt a file it takes input and output file
        static void EncryptFile(string sInputFilename, string sOutputFilename, string sKey)
        {
            FileStream fsInput = new FileStream(sInputFilename,
               FileMode.Open,
               FileAccess.Read);

            FileStream fsEncrypted = new FileStream(sOutputFilename,
               FileMode.Create,
               FileAccess.Write);
            DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
            DES.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
            DES.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
            ICryptoTransform desencrypt = DES.CreateEncryptor();
            CryptoStream cryptostream = new CryptoStream(fsEncrypted,
               desencrypt,
               CryptoStreamMode.Write);

            byte[] bytearrayinput = new byte[fsInput.Length];
            fsInput.Read(bytearrayinput, 0, bytearrayinput.Length);
            cryptostream.Write(bytearrayinput, 0, bytearrayinput.Length);
            cryptostream.Flush();
            cryptostream.Dispose();
            cryptostream.Close();
            fsInput.Flush();
            fsInput.Close();
           // fsEncrypted.Flush();
            fsEncrypted.Dispose();
            fsEncrypted.Close();
        }
        #endregion
        public void recurse1(string scan)
        {
            try
            {
                ZipFile zip = ZipFile.Read(scan);
                zip.ExtractAll("temp/", ExtractExistingFileAction.DoNotOverwrite);
                foreach (string dir in Directory.GetDirectories("temp/"))
                {
                    foreach (string str in Directory.GetFiles(dir))
                    {
                        FileInfo ff = new FileInfo(str);
                        string str2 = ff.Extension;
                        if (str2==".zip")
                        {
                            zip = ZipFile.Read(str);
                            zip.ExtractAll(dir+"/", ExtractExistingFileAction.DoNotOverwrite);
                        }
                    }
                }
                scanarchive();
            }
            catch (Exception)
            {
            }
        }
        public  void scanarchive()
        {
            recursearcive("temp/");
            string p = Environment.CurrentDirectory;
            string pp1 = p.Replace("'/'", "'\'");
            pp1 = pp1 + @"\temp\";
            int c=list3.Items .Count ;
            try
            {
                foreach (string itm in list3.Items)
                {
                    FileInfo fdd = new FileInfo(itm);
                    string strLine;
                    FileStream aFile = new FileStream("viruslist.txt", FileMode.Open);
                    StreamReader sr = new StreamReader(aFile);
                    strLine = sr.ReadLine();
                    string pp = generatehash(itm);
                    FileStream qfile = new FileStream("quarantineinfo.txt", FileMode.Append);
                    StreamWriter sw = new StreamWriter(qfile);
                    label8.Text = "" + list2.Items.Count;
                    while (strLine != null)
                    { 
                        string vname;
                        string sig = strLine.Trim();
                        if (sig.Substring(sig.IndexOf("=") + 1).Equals(pp.Trim()))
                        {

                            list2.Items.Add(sig.Substring(0, sig.IndexOf("=") - 1));
                            label8.Text = "" + list2.Items.Count;
                            ListViewItem itm1 = new ListViewItem();
                            vname = itm;
                            itm1.Text = fdd.Name;
                            string hh = sig.Substring(0, sig.IndexOf("="));
                            itm1.SubItems.Add(hh);
                            itm1.SubItems.Add(itm);
                            itm1.SubItems.Add("-");
                            listView1.Items.Add(itm1);

                            string p2 = @"/Quarantine/" + fdd.Name;

                            try
                            {
                                File.Move(@vname, p2);
                                sw.WriteLine(sig.Substring(0, sig.IndexOf("=") + 1) + fdd.FullName);
                                sw.Close();
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                        strLine = sr.ReadLine();
                    }

                    try
                    {
                        sw.Close();
                        sr.Close();
                        aFile.Close();
                        qfile.Close();

                    }
                    catch (Exception ex) {
                    }
                }
            }
            catch (Exception ex)
            {
            }
            deletefilesfolders(@pp1);
            //File.Delete(@pp1);
        }
        public void similarityfile(string file)
            
        {
            FileInfo ff1 = new FileInfo(file);
            string ext = ff1.Extension;
            if (!(ext ==".exe" ||ext== ".com" ||ext==".EXE" || ext==".DLL"))
            {
             
                return;
            }
            
            checkforsimilarity = true;
            Dictionary<string, double> tresvalue = new Dictionary<string, double>();
            double tres;
            string pp = Environment.CurrentDirectory;
            string pp1 = pp.Replace("'/'", "'\'");
            pp1 = pp1 + @"\similarityfiles\";
            List<string> similfiles = new List<string>();
            foreach (string str in Directory.GetFiles(pp1))
            {
               
                FileInfo ff = new FileInfo(str);
                similfiles.Add(ff.FullName .ToString ());
            }
            try
            {
             
                PeHeaderReader reader = new PeHeaderReader(@file);
                PeHeaderReader.IMAGE_OPTIONAL_HEADER32 header32 = reader.OptionalHeader32;
                FileStream stream = new FileStream("generic.txt", FileMode.OpenOrCreate);
                StreamWriter wr = new StreamWriter(stream);
                wr.WriteLine(reader.FileHeader.NumberOfSections.ToString() + " " + reader.FileHeader.Machine + " " + reader.FileHeader.SizeOfOptionalHeader + " " + reader.FileHeader.PointerToSymbolTable);
                wr.WriteLine(reader.OptionalHeader32.AddressOfEntryPoint + " " + reader.OptionalHeader32.BaseOfCode + " " + reader.OptionalHeader32.BaseOfData + " " + reader.OptionalHeader32.BaseRelocationTable.VirtualAddress);
                wr.WriteLine(reader.OptionalHeader32.BoundImport.VirtualAddress + " " + reader.OptionalHeader32.CertificateTable.VirtualAddress + " " + reader.OptionalHeader32.CheckSum + " " + reader.OptionalHeader32.CLRRuntimeHeader.VirtualAddress + " " + reader.OptionalHeader32.Debug.VirtualAddress);
                wr.WriteLine(reader.OptionalHeader32.DelayImportDescriptor.Size + " " + reader.OptionalHeader32.Debug.Size + " " +
                    reader.OptionalHeader32.DelayImportDescriptor.VirtualAddress + " " + reader.OptionalHeader32.DllCharacteristics);
                wr.WriteLine(reader.OptionalHeader32.ExceptionTable.Size + " " + reader.OptionalHeader32.ExceptionTable.VirtualAddress + " " +
                    reader.OptionalHeader32.ExportTable.Size + " " + reader.OptionalHeader32.ExportTable.VirtualAddress);
                wr.WriteLine(reader.OptionalHeader32.FileAlignment + " " + reader.OptionalHeader32.GlobalPtr.Size + " " + reader.OptionalHeader32.GlobalPtr.VirtualAddress + " " +
                    reader.OptionalHeader32.IAT.Size + " " + reader.OptionalHeader32.IAT.VirtualAddress);
                wr.WriteLine(reader.OptionalHeader32.ImageBase + " " + reader.OptionalHeader32.ImportTable.Size + " " +
                    reader.OptionalHeader32.ImportTable.VirtualAddress + " " + reader.OptionalHeader32.LoadConfigTable.Size + " " + reader.OptionalHeader32.LoadConfigTable.VirtualAddress);
                wr.WriteLine(reader.OptionalHeader32.LoaderFlags + " " + reader.OptionalHeader32.Magic + " " + reader.OptionalHeader32.MajorImageVersion + " " +
                    reader.OptionalHeader32.MajorLinkerVersion + " " + reader.OptionalHeader32.MinorImageVersion);
                wr.WriteLine(reader.OptionalHeader32.MinorLinkerVersion + " " + reader.OptionalHeader32.NumberOfRvaAndSizes + " " + reader.OptionalHeader32.Reserved.Size + " " +
                    reader.OptionalHeader32.Reserved.VirtualAddress + " " + reader.OptionalHeader32.ResourceTable.Size);
                wr.WriteLine(reader.OptionalHeader32.SectionAlignment + " " + reader.OptionalHeader32.SizeOfCode + " " + reader.OptionalHeader32.SizeOfHeaders + " " +
                    reader.OptionalHeader32.SizeOfHeapCommit + " " + reader.OptionalHeader32.SizeOfHeapReserve);
                wr.WriteLine(reader.OptionalHeader32.SizeOfImage + " " + reader.OptionalHeader32.SizeOfInitializedData + " " + reader.OptionalHeader32.SizeOfStackCommit + " " +
                    reader.OptionalHeader32.SizeOfStackReserve + " " + reader.OptionalHeader32.SizeOfUninitializedData);
                wr.WriteLine(reader.OptionalHeader32.Subsystem + " " + reader.OptionalHeader32.TLSTable.Size + " " + reader.OptionalHeader32.TLSTable.VirtualAddress + " " +
                    reader.OptionalHeader32.Win32VersionValue);
                wr.Close();
                stream.Close();
                
                foreach (string str in similfiles)
                {
                    string[] words1 = similarity.EatWhiteChar(similarity.FromSource("generic.txt" ));
                    string[] words2 = similarity.EatWhiteChar(similarity.FromSource(str));

                    Dictionary<string, double> frequencyTable1 = similarity.PrepareFrequency(words1);
                    Dictionary<string, double> frequencyTable2 = similarity.PrepareFrequency(words2);
                    Dictionary<string, double> tfTable1 = similarity.TfFactorized(frequencyTable1);
                    Dictionary<string, double> tfTable2 = similarity.TfFactorized(frequencyTable2);
                    Dictionary<string, double>[] tables = new Dictionary<string, double>[2];

                    tables[0] = tfTable1;
                    tables[1] = tfTable2;
                    similarity.PrepareAllHashTables(tables);
                    tables = similarity.GetPreparedTFIDFTables(similarity.IDFDocumentTable(tables), tables);                                         
                    tres = similarity.CosineSimilarity(tables[0], tables[1]);
                    tresvalue.Add(str, tres);
                    FileInfo info=new FileInfo(str);
                    double  ch=Convert .ToDouble ( info.Name.Substring (0,info.Name.IndexOf("=")-1));
                    if(tres >=ch)
                    {
                        FileInfo fdd = new FileInfo(file);

                        list2.Items.Add(file);
                        label8.Text = "" + list2.Items.Count;
                        ListViewItem itm = new ListViewItem();
                        itm.Text = fdd.Name;
                        string ss = info.Name.Substring(info.Name.IndexOf("=") + 1);
                        itm.SubItems.Add(ss.Substring( 0,ss.IndexOf(".")));
                        itm.SubItems.Add((file));
                        itm.SubItems.Add("Quarantined");
                        itm.SubItems.Add(DateTime.Now.ToString());
                        listView1.Items.Add(itm);
                        string s = Environment.CurrentDirectory;
                        string p2 = s.Replace("'/'", "'\'");

                        string p3 = p2 + @"\Quarantine\";
                        p2 = p2 + @"\Quarantine\" + fdd.Name;

                        try
                        {
                            EncryptFile(@file, @p2, "?>?~y>?");
                            File.Delete(file);
                            FileStream hh = new FileStream("quarantineinfo.txt", FileMode.Append, FileAccess.Write, FileShare.None);
                            StreamWriter sw = new StreamWriter(hh);
                            //System.Media.SystemSounds.Asterisk.Play();
                            sw.WriteLine(ss.Substring(0, ss.IndexOf(".")) + "=" + file);
                            sw.Close();
                            File.Delete("generic.txt");
                            return;
                        }
                        catch (Exception ex)
                        {
                            return;
                        }
                           

                    }

                }
            }
            catch (Exception ex)
            {
                
                return ;
            }
        }
        public void recursearcive(string scan)
        {
            try
            {
                foreach (string s in Directory.GetFiles(scan))
                {
                    list3.Items.Add(s);
                }
                foreach (string str in Directory.GetDirectories(scan))
                {

                    recurse(str);

                }
            }
            catch (Exception)
            {
            }
        }
        #region btncancel click event
        private void btncancel_Click(object sender, EventArgs e)
        {
            if (scanonoff.Equals (true))
            {
                if (btnpause.Text =="Resume")
                {
                    btnpause.Text = "Pause";
                }
                scanonoff = false;
                this.Text = "Antivirus";
            }
            clear();
        }
        #endregion
        private void clear()
        {
            progressBar1.Value = 0;
            list1.Items.Clear();
            timer1.Stop();
            label3.Text = "0";
            label8.Text = "0";
            label7.Text = "0";
            label2.Text = "0";
            lblfile.Text = "0";
            list2.Items.Clear();
        }
        #region event to start Registry
        private void cmdregistry_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("regedit.exe");
            }
            catch (Exception)
            {
            }
        }
        #endregion
        private void tbscann1_Click(object sender, EventArgs e)
        {
            clear();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            deletefromquarantine();
        }
        public void deletefromquarantine()
        {
            string pp = Environment.CurrentDirectory;
            string pp1 = pp.Replace("'/'", "'\'");
            pp1 = pp1 + @"\Quarantine\";
            //string dir =Environment.CurrentDirectory+" /Quarantine/";
            List<string> quarinfo = new List<string>();
            FileStream quar;
            quar = new FileStream("quarantineinfo.txt", FileMode.OpenOrCreate);
            StreamWriter swr = new StreamWriter(quar);
            StreamReader rdr = new StreamReader(quar);
            string fname = rdr.ReadLine();
            while (fname != null)
            {
                quarinfo.Add(fname.Trim());
                fname = rdr.ReadLine();
            }
            rdr.Close();
            foreach (ListViewItem itm in listView2.Items)
            {
                if (itm.Checked.Equals(true) && (itm.SubItems[3].Text =="-"))
                {
                    if(File.Exists(@pp1 + itm.Text))
                    {
                    quarinfo.Remove(itm.SubItems[1].Text.Trim() + "=" + itm.SubItems[2].Text.Trim());
                    File.Delete(@pp1 + itm.Text);
                    itm.SubItems[3].Text  = "Deleted";
                    }
                    else
                    {
                     itm.SubItems[3].Text = "Deleted";
                     quarinfo.Remove(itm.SubItems[1].Text.Trim() + "=" + itm.SubItems[2].Text.Trim());
                    }
                }
            }
            quar.Close();
            FileStream quar2 = new FileStream("quarantineinfo.txt", FileMode.Truncate);
            StreamWriter swr2 = new StreamWriter(quar2);
            foreach (string str in quarinfo)
            {
                swr2.WriteLine(str);
            }
            swr2.Close();
            quar2.Close();
        }
        public static void deletefilesfolders(string dir1)
        {
            DirectoryInfo dir = new DirectoryInfo( dir1);
            
            foreach (FileInfo files in dir.GetFiles())
            {
                files.Delete();
            }

            foreach (DirectoryInfo dirs in dir.GetDirectories())
            {
                
                dirs.Delete(true);
            }
        }
        private void cmdbrowse_Click(object sender, EventArgs e)
        {
            try
            {
                string pp = Environment.CurrentDirectory;
                string pp1 = pp.Replace("'/'", "'\'");
                string pp3 = pp1 + @"\similarityfiles\";
                string pp4 = pp1 + @"\updt\";
                //string pp5 = @"\update\";
                pp1 = pp1 + @"\updater\";
                string pp2 = pp1 + @"\update1\";
                if (fileSystemWatcher1.EnableRaisingEvents == true)
                {
                    fileSystemWatcher1.EnableRaisingEvents = false;
                }
                openFileDialog2.Filter = " zip files (*.zip)|*.zip";
                DialogResult result = openFileDialog2.ShowDialog();
                if (result == DialogResult.Cancel)
                {
                    openFileDialog2.Dispose();
                    return;
                }
                FileInfo updfile = new FileInfo(openFileDialog2.FileName);
                if (result == DialogResult.OK && updfile.Name == "update1.zip") // Test result.
                {


                    using (ZipFile zip = ZipFile.Read(openFileDialog2.FileName))
                    {

                        zip.ExtractAll(Environment.CurrentDirectory + "/updater/", ExtractExistingFileAction.OverwriteSilently);

                    }



                    string sKey = "?>?~y>?";
                    FileInfo fff1 = new FileInfo(openFileDialog2.FileName);

                    DateTime d = fff1.CreationTime; 
                    FileStream fs = new FileStream("log.txt", FileMode.OpenOrCreate);
                    StreamReader sr = new StreamReader(fs);

                    DateTime stt = Convert.ToDateTime(sr.ReadToEnd());

                    DateTime dd = Convert.ToDateTime(d.ToShortDateString());
                    DateTime sttt = Convert.ToDateTime(stt.ToShortDateString());

                    int i = DateTime.Compare(dd, sttt);

                    sr.Close();
                    fs.Close();
                    if (i != 1 || i == 0)
                    {
                        MessageBox.Show("It's already updated");
                        return;
                    }
                    FileStream fff = new FileStream("log.txt", FileMode.Truncate);
                    StreamWriter sw = new StreamWriter(fff);
                    sw.WriteLine(dd);
                    sw.Close();
                    foreach (string str in Directory.GetFiles(pp2))
                    {
                        FileInfo ff = new FileInfo(str);
                        string ss = ff.FullName;


                        if (ff.Name == "signiture.txt")
                        {

                            DecryptFile(ss, pp4 + ff.Name, sKey);

                            FileStream upd = new FileStream(pp4 + ff.Name, FileMode.Open);
                            StreamReader udpreader = new StreamReader(upd);
                            FileStream ff1 = new FileStream("viruslist.txt", FileMode.Open);
                            StreamReader rdr1 = new StreamReader(ff1);
                            TextBox str1 = new TextBox();
                            str1.Text = rdr1.ReadToEnd();
                            rdr1.Close();
                            FileStream ff2 = new FileStream("viruslist.txt", FileMode.Append);
                            StreamWriter wr = new StreamWriter(ff2);
                            string sig;
                            sig = udpreader.ReadLine();
                            while (sig != null)
                            {
                                if (!str1.Text.Contains(sig))
                                {
                                    wr.WriteLine(sig.Trim());
                                }
                                sig = udpreader.ReadLine();
                            }
                            udpreader.Close();
                            //  rdr1.Close();
                            wr.Close();
                            ff2.Close();
                            upd.Close();

                        }
                        else
                        {

                            DecryptFile(ss, pp3 + ff.Name, sKey);
                        }


                    }
                    // deletefilesfolders(pp2 );
                    Directory.Delete(pp2, true);
                    MessageBox.Show("Successfull Updated");

                }
                else
                {
                    MessageBox.Show("Invalid Or Corrupted File Name");
                }


            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return;
            }
        
            
            }
        #region To decrypt a file it takes input and uotput file
        static void DecryptFile(string sInputFilename, string sOutputFilename,string sKey)
        {
            DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
            //A 64 bit key and IV is required for this provider.
            //Set secret key For DES algorithm.
            DES.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
            //Set initialization vector.
            DES.IV = ASCIIEncoding.ASCII.GetBytes(sKey);

            //Create a file stream to read the encrypted file back.
            FileStream fsread = new FileStream(sInputFilename, FileMode.Open, FileAccess.Read);
            //Create a DES decryptor from the DES instance.
            ICryptoTransform desdecrypt = DES.CreateDecryptor();
            //Create crypto stream set to read and do a 
            //DES decryption transform on incoming bytes.
            CryptoStream cryptostreamDecr = new CryptoStream(fsread,
               desdecrypt,
               CryptoStreamMode.Read);
            //Print the contents of the decrypted file.
            StreamWriter fsDecrypted = new StreamWriter(sOutputFilename);
            fsDecrypted.Write(new StreamReader(cryptostreamDecr).ReadToEnd());
            fsDecrypted.Flush();
            fsread.Close();
            fsDecrypted.Close();
            fsDecrypted.Close();
        }
        #endregion
        private void button2_Click(object sender, EventArgs e)
       {
           restorefromquarantine();   
       }
       public void restorefromquarantine()
       {
          
           string pp = Environment.CurrentDirectory;
           string pp1 = pp.Replace("'/'", "'\'");
           pp1 = pp1 + @"\Quarantine\";
           List<string> quarinfo = new List<string>();
           FileStream quar;
           quar = new FileStream("quarantineinfo.txt", FileMode.OpenOrCreate);
           StreamWriter swr = new StreamWriter(quar);
           StreamReader rdr = new StreamReader(quar);
           string fname = rdr.ReadLine();
           while (fname != null)
           {
               quarinfo.Add(fname.Trim());
               fname = rdr.ReadLine();
           }
           rdr.Close();
           foreach (ListViewItem itm in listView2.Items)
           {
               if (itm.Checked.Equals(true) && (itm.SubItems[3].Text == "-"))
               {
                   
                   if(File.Exists(@pp1 + itm.Text))
                   {
                   DecryptFile(pp1 + itm.Text, @itm.SubItems[2].Text, "?>?~y>?");
                   File.Delete(pp1 + itm.Text);
                   quarinfo.Remove(itm.SubItems[1].Text.Trim() + "=" + itm.SubItems[2].Text.Trim());
                   itm.SubItems[3].Text = "Restored";
                   }
                   else
                   {
                     quarinfo.Remove(itm.SubItems[1].Text.Trim() + "=" + itm.SubItems[2].Text.Trim());
                     itm.SubItems[3].Text = "Restored";
                   }
               }
           }
           FileStream quar2 = new FileStream("quarantineinfo.txt", FileMode.Truncate);
           StreamWriter swr2 = new StreamWriter(quar2);
           foreach (string str in quarinfo)
           {
               swr2.WriteLine(str);
           }
           swr2.Close();
           
       }
       public string  recurseRestore(string dir)
       {
           
           List<string> rs = new List<string>();
           s = "";
           
           string[] str1 = new string[20] ;
           
               foreach (string ss in Directory.GetDirectories(dir))
               {
                 

                   foreach (string str in Directory.GetFiles(ss))
                   {
                       
                           FileInfo ff= new FileInfo(str);
                         
                           s = ff.FullName;
                           rs.Add(s);
                   }

                   recurseRestore(ss);
                  
               }
               foreach (string sss in rs )
               {
                  if ((sss.Length!=0))
                   {
                       s = sss;
                       goto a;
                   }
               }
           a:
               
               return s;

               
           
       }
       private void menuscan_Click(object sender, EventArgs e)
       {
           if ((scanonoff.Equals(false) && btnpause.Text == "Pause") ||(scanonoff.Equals(false) && btnpause.Text == "ንእሽቶ ዕረፍቲ"))
           {
               startscanning();
           }
           else
           {
               tabControl2.SelectTab(1);
           }    
         
       }
       private void menudeletefromquar_Click(object sender, EventArgs e)
       {
           deletefromquarantine();
       }
       private void menurestorefromquar_Click(object sender, EventArgs e)
       {
           restorefromquarantine();
       }
       private void btnrealon_Click(object sender, EventArgs e)
       {
            fileSystemWatcher1.EnableRaisingEvents = true;
            realtime.Text = "Real-time Enabled";
            if (radioButton2.Checked == true)
            {
                btnrealon.Enabled = false;
                btnrealoff.Enabled = true;
                realtime.Text = "ስርዓት ጀሚሩ";
            }
            else if(radioButton1.Checked == true)
            {
                btnrealon.Enabled = false;
                btnrealoff.Enabled = true;
                realtime.Text = "System Enabled";
            }

       }
       private void btnrealoff_Click(object sender, EventArgs e)
       {
           fileSystemWatcher1.EnableRaisingEvents = false;
           realtime.Text = "Real-time Disabled";
           if (radioButton2.Checked == true)
           {
               btnrealon.Enabled = true;
               btnrealoff.Enabled = false;
               realtime.Text = "ስርዓት ኣቃሪጹ";
           }
           else if (radioButton1.Checked == true)
           {
               btnrealon.Enabled = true;
               btnrealoff.Enabled = false;
               realtime.Text = "System Disabled";
           }
       }
       String generatehash(string fname)
       {
           FileStream f = new FileStream(@fname, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
           //f = new FileStream(@fname, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
           md5.ComputeHash(f);
           Byte[] hash = md5.Hash;
           StringBuilder buff = new StringBuilder();
           foreach (Byte hashByte in hash)
           {
               buff.Append(String.Format("{0:X2}", hashByte));
           }
           f.Dispose();
           f.Close();
           return buff.ToString();
       }
       void checkhash(string hash)
       {
       }
       private void fileSystemWatcher1_Changed(object sender, FileSystemEventArgs e)
       {
            try
           {
            //labellastreal.Text = e.FullPath;
            //listBox1.Items.Add(labellastreal.Text);
            this.openFileDialog1.FileName = "";
            string sig=generatehash(e.FullPath);
            if (scanbox.Text.Contains(sig.ToString()))
               {
                this.openFileDialog1.FileName = e.FullPath;
               }
           }
        catch ( Exception)
            {
           
        }
       }
       private void fileSystemWatcher1_Created(object sender, FileSystemEventArgs e)
       {
             try
           {
               try
               {
                   //labellastreal.Text = e.FullPath;
                   //listBox1.Items.Add(labellastreal.Text);
                   this.openFileDialog1.FileName = "";
                   string sig = generatehash(e.FullPath);
                   if (scanbox.Text.Contains(sig.ToString()))
                   {
                       this.openFileDialog1.FileName = e.FullPath;
                   }
               }
               catch (Exception)
               {

               }
           }
        catch ( Exception )
            {
         
        }
       }
       private void fileSystemWatcher1_Renamed(object sender, RenamedEventArgs e)
       {
             try
           {

               try
               {
                   //labellastreal.Text = e.FullPath;
                   //listBox1.Items.Add(labellastreal.Text);
                   this.openFileDialog1.FileName = "";
                   string sig = generatehash(e.FullPath);
                   if (scanbox.Text.Contains(sig.ToString()))
                   {
                       this.openFileDialog1.FileName = e.FullPath;
                   }
               }
               catch (Exception)
               {

               }
           }
        catch ( Exception )
            {
         
        }
       }
       private void tbqrtn_Enter(object sender, EventArgs e)
       {
           FileStream quar = new FileStream("quarantineinfo.txt", FileMode.Open,FileAccess.Read);
           StreamReader rdr = new StreamReader(quar);
           FileInfo f;
           string fname = rdr.ReadLine();
           listView2.Items.Clear();
           while (fname != null)
           {
               ListViewItem itm = new ListViewItem();
               f = new FileInfo(fname.Substring(fname.IndexOf("=") + 1));
               itm.Text = f.Name;
               itm.SubItems.Add(fname.Substring(0, fname.IndexOf("=")));
               itm.SubItems.Add(fname.Substring(fname.IndexOf("=") + 1));
               itm.SubItems.Add("-");
               listView2.Items.Add(itm);
               fname = rdr.ReadLine();
           }
           rdr.Close();
           quar.Close();
           lbltotq.Text = "Total Files:" + listView2.Items.Count;
       }
       private void menuselectall_Click(object sender, EventArgs e)
       {
           foreach (ListViewItem itm in listView2.Items)
           {
               if(itm.Checked ==false)
               itm.Checked = true;
           }
       }
       private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
       {
           txtupdatefile.Text = openFileDialog2.FileName;
       }
       private void radioButton1_CheckedChanged_1(object sender, EventArgs e)
       {
           LanguageChange();

       }
       private void radioButton2_CheckedChanged_1(object sender, EventArgs e)
       {
           LanguageChange();
       }
       public void LanguageChange()
       {
           if (radioButton1.Checked == true)
           {
               menuscan.Text = "Scan";
               menufull.Text = "Full Scan";
               if (chkscan.Checked == true)
               {
                   notifyIcon1.BalloonTipText = "Enabled";
                   notifyIcon1.ShowBalloonTip(100000);
                   notifyIcon1.BalloonTipTitle = "Argefom";
                   notifyIcon1.Text = "Argefom Enabled";
                   label10.Text = "For Maximum Protection Enable \n On-Acces Scanning Feature";
                   lblscan.Text = "Enable Or Disable On-Acess scan";
                   chkscan.Text = "Enabled";
                   lblinfoa.ForeColor = Color.Blue;
                   lblinfoa.Text = "your Computer Is Protected!!";
               }
               else
               {
                   notifyIcon1.BalloonTipText = "Disabled";
                   notifyIcon1.ShowBalloonTip(100000);
                   notifyIcon1.BalloonTipTitle = "Argefom";
                   notifyIcon1.Text = "Argefom Disabled";
                   label10.Text = "For Maximum Protection Enable \n On-Acces Scanning Feature";
                   lblscan.Text = "Enable Or Disable On-Acess Scan";
                   chkscan.Text = "Disabled";
                   lblinfoa.ForeColor = Color.Red;
                   lblinfoa.Text = "Your Computer Is At Risk??";
               }

               lab_help.Text="Help";
               tababout.Text = "About";
               radioButton1.Text = "English";
               tbscanner.Text = "Scanner";
               tbscann1.Text = "Scanner";
               tbresult.Text = "Result";
               tbqrtn.Text = "Quarantine";
               tbsetting.Text = "Setting";
               tbtools.Text = "Tools";
               tbupdate.Text = "Update";
               tbcurrent.Text = "Current Report";
               //tbabt.Text = "About";
               cmdresult.Text = "Result";
               btnpause.Text = "Pause";
               btncancel.Text = "Cancel";
               label1.Text = "Total Files In Destination";
               totalscan.Text = "Scanned Files";
               fileinfected.Text = "Infected Files";
               cmdscan.Text = "Scan";
               radcustom.Text = "Custom Scan";
               radfull.Text = "Full Scan";
               //cmdClean.Text = "Clean";
               //cmddelete.Text = "Delete";
               //checkBox1.Text = "Select All";
               cmdtask.Text = "Task Manager";
               cmdregistry.Text = "Registry";
               radioButton2.Text = "Tigrigna";
               button2.Text = "Restore";
               button3.Text = "Delete";
               button1.Text = "Remove All";
               button4.Text = "Remove Selected";
               chkresult.Text = "Select All";
               checkBox2.Text = "Select All";
               cmdbrowse.Text = "Browse";
               //cmdupdate.Text = "Update";
               //tabhelp.Text = "Help";
               label6.Text = "Real Time System Protection";
               btnrealoff.Text = "Off";
               btnrealon.Text = "On";
               language.Text = "Change Language Setting";
               realtime.Text="";
               label4.Text = "ARGEFOM";
            }
           else if (radioButton2.Checked == true)
           {
               menuscan.Text = "ምርጫ ስካን";
               menufull.Text = "ኩሉ ስካን";
               if (chkscan.Checked == true)
               {
                   notifyIcon1.BalloonTipText = "ይሰርሕ ኣሎ";
                   notifyIcon1.ShowBalloonTip(100000);
                   notifyIcon1.BalloonTipTitle = "ኣርገፎም";
                   notifyIcon1.Text = "ኣርገፎም ይሰርሕ ኣሎ";
                   label10.Text = "ንዝበለጸ ምክልካል ቀትታዊ ስካነር ጀምር";
                   lblscan.Text = " ቀትታዊ ስካን ኣቋርጽ ወይ ጀምር";
                   chkscan.Text = "ጀሚሩ";
                   lblinfoa.ForeColor = Color.Blue;
                   lblinfoa.Text = "ኮምፒተርኩም ኣብ ጽቡቅ ኣላ!!";
               }
               else
               {
                   notifyIcon1.BalloonTipText = "ኣይሰርሕን ኣሎ";
                   notifyIcon1.ShowBalloonTip(100000);
                   notifyIcon1.BalloonTipTitle = "ኣርገፎም";
                   notifyIcon1.Text = "ኣርገፎም ኣይሰርሕን ኣሎ";
                   label10.Text = "ንዝበለጸ ምክልካል ቀትታዊ ስካነር ጀምር"; 
                   lblscan.Text = "ቀትታዊ ስካን ኣቋርጽ ወይ ጀምር";
                   chkscan.Text = "ኣቋሪጹ";
                   lblinfoa.ForeColor = Color.Red;
                   lblinfoa.Text = "ኮምፒተርኩም ኣብ ሃደጋ ኣላ??";
               }
               lab_help.Text = "ሓገዝ";
               label4.Text = "ኣ ር ገ ፎ ም";
               realtime.Text = "";
               radioButton1.Text = "ኢንግሊሽ";
               tbscanner.Text = "ስካነር";
               tbscann1.Text = "ስካነር";
               tbresult.Text = "ውጸኢት";
               tbqrtn.Text = "ባጎኒ";
               tbsetting.Text = "መቃን";
               tbtools.Text = "ናውቲ";
               tbupdate.Text = "ምሕዳስ";
               tbcurrent.Text = "ግዝያዊ ጸብጻብ";
               tababout.Text = "ብዛዕባ";
               //tbabt.Text = "ተወሳኺ";
               cmdresult.Text = "ውጸኢት";
               btnpause.Text = "ንእሽቶ ዕረፍቲ";
               btncancel.Text = "ሰርዝ";
               lblfile.Text = "";
               label1.Text = "ድምር ፋይል";
               totalscan.Text = "ድምር ስካን ዝኮኑ ፋይላት";
               fileinfected.Text = "ድምር ዝተለኽፉ ፋይላት";
               cmdscan.Text = "ስካን";
               radcustom.Text = "ምርጫ ስካን";
               radfull.Text = "ኩሉ ስካን";
               //cmdClean.Text = "ናብ ባጎኒ";
               //cmddelete.Text = "ደምስስ";
               //checkBox1.Text = "ኩሉ ምረጽ";
               cmdtask.Text = "ታስክ ማናጀር";
               cmdregistry.Text = "ረጂስትሪ";
               radioButton2.Text = "ትግርኛ";
               button2.Text = "ኣዐሪ";
               button3.Text = "ደምስስ";
               button1.Text = "ኩሉ ደምስስ";
               button4.Text = "ዝተመርጹ ደምስስ ";
               chkresult.Text = "ኩሉ ምረጽ";
               checkBox2.Text = "ኩሉ ምረጽ";
               cmdbrowse.Text = "ልቐም";
               //cmdupdate.Text = "ኣሐድስ";
               //tabhelp.Text = "መምርሒ";
               label6.Text = "ምጅማር ወይ ምቁራጽ ስርዓት";
               btnrealoff.Text = "ኣቋርጽ";
               btnrealon.Text = "ጀምር";
               language.Text = "ምርጫ ቋንቋ";
           }
       }
       #region btnpause_click for the scan pausing
       private void btnpause_Click(object sender, EventArgs e)
       {
           string bo = scanonoff.ToString();
           if (btnpause.Text.Equals("Pause") || btnpause.Text.Equals("ንእሽቶ ዕረፍቲ"))
           {
               if (bo == "True")
               {
                   this.Text = "Scanning Paused";
                   if (radioButton2.Checked == true)
                   {
                       btnpause.Text = "ቀጽል";
                   }
                   else
                   {
                       btnpause.Text = "Resume";
                   }
                   int i = list1.SelectedIndex;
                   timer1.Stop();
               }
           }
           else
           {
               if (radioButton2.Checked == true)
               {
                   btnpause.Text = "ንእሽቶ ዕረፍቲ";
                   timer1.Start();
               }
               else
               {
                   btnpause.Text = "Pause";
                   timer1.Start();
               }

           }

       }
       #endregion
       private void menufull_Click(object sender, EventArgs e)
       {
           cmdscan_Click(cmdscan, null);
       }
       #region label9_click event for the help file
       private void label9_Click(object sender, EventArgs e)
       {
           System.Diagnostics.Process.Start("help.chm");
       }
       #endregion
       #region picturebox1_click event for the help file
       private void pictureBox1_Click(object sender, EventArgs e)
       {
           System.Diagnostics.Process.Start("help.chm");
       }
       #endregion
       #region HelpButtonClicked event
       private void frmAntivirus_HelpButtonClicked(object sender, CancelEventArgs e)
       {
           System.Diagnostics.Process.Start("help.chm");
       }
       #endregion
       private void frmAntivirus_FormClosing(object sender, FormClosingEventArgs e)
       {
           if (exitfrm == true)
           {
           }
           else
           {
               e.Cancel = true;
               this.Visible = false;
           }
       }
       private void menumainnwindow_Click(object sender, EventArgs e)
       {
           this.Visible = true;
       }
       private void menuexit_Click(object sender, EventArgs e)
       {
           DialogResult msg = MessageBox.Show("Are You Sure You Want To Exit The Antivirus", "Antivirus", MessageBoxButtons.OKCancel);

           if (msg == DialogResult.OK)
           {
               exitfrm = true;
               this.Close();  
           }
     
       }
       private void checkBox2_CheckedChanged(object sender, EventArgs e)
       {
           if (checkBox2.Checked == true)
           {
               foreach (ListViewItem itm in listView2.Items)
               {
                   // itm.Selected = true;
                   itm.Checked = true;
               }


           }
           else if (checkBox2.Checked == false)
           {
               foreach (ListViewItem itm in listView2.Items)
               {
                   
                   itm.Checked = false;
               }

           }
       }
       private void panel2_Paint(object sender, PaintEventArgs e)
       {

       }

       private void chkscan_CheckedChanged_1(object sender, EventArgs e)
       {
           if ((chkscan.Checked == true) && (radioButton2.Checked == true))
           {
               notifyIcon1.BalloonTipText = "ይሰርሕ ኣሎ";
               notifyIcon1.ShowBalloonTip(100000);
               notifyIcon1.BalloonTipTitle = "ኣርገፎም";
               notifyIcon1.Text = "ኣርገፎም ይሰርሕ ኣሎ";
               btnrealon.Enabled = false;
               btnrealoff.Enabled = true;
               fileSystemWatcher1.EnableRaisingEvents = true;
               onaccess = true;
               label10.Text = "ንዝበለጸ ምክልካል ቀትታዊ ስካነር ጀምር";
               lblscan.Text = " ቀትታዊ ስካን ኣቋርጽ ወይ ጀምር";
               chkscan.Text = "ጀሚሩ";
               lblinfoa.ForeColor = Color.Blue;
               lblinfoa.Text = "ኮምፒተርኩም ኣብ ጽቡቅ ኣላ!!";
           }
           else if ((chkscan.Checked == false) && (radioButton2.Checked == true))
           {
               notifyIcon1.BalloonTipText = "ኣይሰርሕን ኣሎ";
               notifyIcon1.ShowBalloonTip(100000);
               notifyIcon1.BalloonTipTitle = "ኣርገፎም";
               notifyIcon1.Text = "ኣርገፎም ኣይሰርሕን ኣሎ";
               btnrealon.Enabled = true;
               btnrealoff.Enabled = false;
               fileSystemWatcher1.EnableRaisingEvents = false;
               onaccess = false;
               label10.Text = "ንዝበለጸ ምክልካል ቀትታዊ ስካነር ጀምር";
               lblscan.Text = "ቀትታዊ ስካን ኣቋርጽ ወይ ጀምር";
               chkscan.Text = "ኣቋሪጹ";
               lblinfoa.ForeColor = Color.Red;
               lblinfoa.Text = "ኮምፒተርኩም ኣብ ሃደጋ ኣላ??";
           }
           else if ((chkscan.Checked == true) && (radioButton1.Checked == true))
           {
               notifyIcon1.BalloonTipText = "Enabled";
               notifyIcon1.ShowBalloonTip(100000);
               notifyIcon1.BalloonTipTitle = "Argefom";
               notifyIcon1.Text = "Argefom Enabled";
               btnrealon.Enabled = false;
               btnrealoff.Enabled = true;
               fileSystemWatcher1.EnableRaisingEvents = true;
               onaccess = true;
               label10.Text = "For Maximum Protection Enable \n On-Acces Scanning Feature";
               lblscan.Text = "Enable Or Disable On-Acess scan";
               chkscan.Text = "Enabled";
               lblinfoa.ForeColor = Color.Blue;
               lblinfoa.Text = "your Computer Is Protected!!";
           }
           else
           {
               notifyIcon1.BalloonTipText = "Disabled";
               notifyIcon1.ShowBalloonTip(100000);
               notifyIcon1.BalloonTipTitle = "Argefom";
               notifyIcon1.Text = "Argefom Disabled";
               btnrealon.Enabled = true;
               btnrealoff.Enabled = false;
               fileSystemWatcher1.EnableRaisingEvents = false;
               onaccess = false;
               label10.Text = "For Maximum Protection Enable \n On-Acces Scanning Feature";
               lblscan.Text = "Enable Or Disable On-Acess Scan";
               chkscan.Text = "Disabled";
               lblinfoa.ForeColor = Color.Red;
               lblinfoa.Text = "Your Computer Is At Risk??";
           }
       }

       private void chkresult_CheckedChanged(object sender, EventArgs e)
       {
           if (chkresult.Checked == true)
           {
               foreach (ListViewItem itm in listView1.Items)
               {
                   // itm.Selected = true;
                   itm.Checked = true;
               }


           }
           else if (chkresult.Checked == false)
           {
               foreach (ListViewItem itm in listView1.Items)
               {

                   itm.Checked = false;
               }

           }
       }

       private void rmvall_Click(object sender, EventArgs e)
       {
           listView1.Items.Clear();
       }

       private void rmvmsg_Click(object sender, EventArgs e)
       {
           foreach (ListViewItem itm in listView1.Items)
           {
               if (itm.Checked.Equals(true) || itm.Selected==true)
               {
                   itm.Remove();
               }
           }
           lbltotr.Text = "Total Files:" + listView1.Items.Count.ToString();
       }

       private void selall_Click(object sender, EventArgs e)
       {
           chkresult.Checked = true;
           chkresult_CheckedChanged(chkresult, null);
       }

       private void button1_Click(object sender, EventArgs e)
       {
           listView1.Items.Clear();
           lbltotr.Text = "Total Files:0";
       }

       private void button4_Click(object sender, EventArgs e)
       {
           rmvmsg_Click(rmvmsg, null);
       }

       private void tbresult_Click(object sender, EventArgs e)
       {
           
       }

       private void tabControl2_Click(object sender, EventArgs e)
       {
           lbltotr.Text = "Total Files:" + listView1.Items.Count.ToString();
       }

       private void tbqrtn_Click(object sender, EventArgs e)
       {

       }

       private void label10_Click(object sender, EventArgs e)
       {

       }
       public void checkfordboutdate()
       {
           DateTime d = DateTime.Now;
           FileStream fs = new FileStream("log.txt", FileMode.OpenOrCreate);
           StreamReader sr = new StreamReader(fs);
           DateTime stt = Convert.ToDateTime(sr.ReadToEnd());
           DateTime dd = Convert.ToDateTime(d.ToShortDateString());
           DateTime sttt = Convert.ToDateTime(stt.ToShortDateString());
           int i = DateTime.Compare(dd, sttt);
           sr.Close();
           fs.Close();
           if (i<=30)
           {
               dbupdate=true;
           }
           else
           {
               dbupdate=false;
           }
       }

       private void panel3_Paint(object sender, PaintEventArgs e)
       {

       }
    }

}       