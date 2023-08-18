using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using System.ServiceProcess;
using System.Diagnostics;

namespace Temporary_Files_Cleaner
{
    public partial class TempCleaner : Form
    {
        private const int AW_VER_NEGATIVE = 0x00040008;
        private const int AW_VER_POSITIVE = 0x00040004;
        private const int AW_SLIDE = 0x00040000;
        private const int AW_HIDE = 0x00010000;

        [DllImport("user32")]
        private static extern bool AnimateWindow(IntPtr hwnd, int time, int flags);

        [DllImport("shell32.dll")]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        [Flags]
        public enum RecycleFlags : uint
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000002,
            SHERB_NOSOUND = 0x00000004
        }

        long TempFilesSize = 0;

        private bool isFirewallEnabled;

        public TempCleaner()
        {
            InitializeComponent();
            isFirewallEnabled = IsFirewallEnabled();
            UpdateFirewallStatus();
        }

        private bool IsFirewallEnabled()
        {
            Process process = new Process();
            process.StartInfo.FileName = "netsh";
            process.StartInfo.Arguments = "advfirewall show currentprofile state";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output.Contains("ON");
        }
        private void ToggleFirewall()
        {
            string command = isFirewallEnabled ? "netsh advfirewall set currentprofile state off" : "netsh advfirewall set currentprofile state on";
            Process.Start("cmd.exe", "/c " + command).WaitForExit();

            isFirewallEnabled = !isFirewallEnabled;
        }

        private void UpdateFirewallStatus()
        {
            firewallStatusLabel.Text = isFirewallEnabled ? "Windows Firewall: Enabled" : "Windows Firewall: Disabled";
        }

        private void TempCleaner_Load(object sender, EventArgs e)
        {
            AnimateWindow(this.Handle, 500, AW_VER_POSITIVE | AW_SLIDE);
            TempFilesSize = 0;

            string szTemporaryFilesPath = Path.GetTempPath();

            var Dir = new DirectoryInfo(szTemporaryFilesPath);

            if (!Dir.Exists)
            {
                MessageBox.Show("Welp... 'Temp' folder is missing somehow!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            foreach (DirectoryInfo dir in Dir.GetDirectories())
            {
                try
                {
                    TempFilesSize += DirectorySize(dir, true);
                }

                catch (Exception)
                {
                    // Ignore folders that are locked or being used.
                }
            }

            foreach (FileInfo szFile in Dir.GetFiles())
            {
                try
                {
                    TempFilesSize += szFile.Length;
                }

                catch (Exception)
                {
                    // Ignore files that are locked or being used.
                }
            }

            lbl_JunkSize.Text = "Temporary Junk: " + BytesToString(TempFilesSize);
        }

        private void guna2ImageButton4_Click(object sender, EventArgs e)
        {
            int result = SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SHERB_NOCONFIRMATION);
            if (result == 0)
            {
                MessageBox.Show("Recycle bin emptied successfully.");
            }
            else
            {
                MessageBox.Show("Failed to empty recycle bin.");
            }
        }

        static long DirectorySize(DirectoryInfo dInfo, bool includeSubDir)
        {
            // Enumerate all the files
            long totalSize = dInfo.EnumerateFiles().Sum(file => file.Length);

            // If Subdirectories are to be included
            if (includeSubDir)
            {
                // Enumerate all sub-directories
                totalSize += dInfo.EnumerateDirectories().Sum(dir => DirectorySize(dir, true));
            }
            return totalSize;
        }

        static String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

            if (byteCount == 0)
            {
                return "0 " + suf[0];
            }

            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));

            double num = Math.Round(bytes / Math.Pow(1024, place), 1);

            return (Math.Sign(byteCount) * num).ToString() + " " + suf[place];
        }

        private void guna2ImageButton5_Click(object sender, EventArgs e)
        {
            TempFilesSize = 0;

            DialogResult dialogResult = MessageBox.Show("Clean all temporary files?", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dialogResult == DialogResult.Yes)
            {
                // --| This is basic "Temp" folder in every Windows Operating System!
                // --| Many users do not know that it contains temporary files that are stored in this folder in time, and can really fill your HDD space!
                // --| This folder can be accessed by typing "%temp%" or "%appdata%" into the search bar. Because is so tricky many users don't know about it!
                string szTemporaryFilesPath = Path.GetTempPath();

                var Dir = new DirectoryInfo(szTemporaryFilesPath);

                if (!Dir.Exists)
                {
                    MessageBox.Show("Welp... 'Temp' folder is missing somehow!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                foreach (DirectoryInfo dir in Dir.GetDirectories())
                {
                    try
                    {
                        TempFilesSize += DirectorySize(dir, true);
                        dir.Delete(true);
                    }

                    catch (Exception)
                    {
                        // Ignore folders that are locked or being used.
                    }
                }

                foreach (FileInfo szFile in Dir.GetFiles())
                {
                    try
                    {
                        TempFilesSize += szFile.Length;
                        szFile.Delete();
                    }

                    catch (Exception)
                    {
                        // Ignore files that are locked or being used.
                    }

                }

                MessageBox.Show("Temporary files have been deleted!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                TempFilesSize = 0;
                lbl_JunkSize.Text = "Temporary Junk: " + TempFilesSize + " KB";

            }

            else if (dialogResult == DialogResult.No)
            {
                return;
            }
        }

        private void guna2PictureBox1_Click(object sender, EventArgs e)
        {
            AnimateWindow(this.Handle, 500, AW_VER_NEGATIVE | AW_SLIDE | AW_HIDE);
            Close();
        }

        private void guna2PictureBox2_Click(object sender, EventArgs e)
        {
            AnimateWindow(this.Handle, 500, AW_VER_NEGATIVE | AW_SLIDE | AW_HIDE);
            WindowState = FormWindowState.Minimized;
            AnimateWindow(this.Handle, 500, AW_VER_POSITIVE | AW_SLIDE);
        }
        Point lastPoint;
        private void guna2Panel1_MouseDown(object sender, MouseEventArgs e)
        {
            lastPoint = new Point(e.X, e.Y);
        }

        private void guna2Panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += e.X - lastPoint.X;
                this.Top += e.Y - lastPoint.Y;
            }
        }

        private void toggleFirewallButton_Click(object sender, EventArgs e)
        {
            ToggleFirewall();
            UpdateFirewallStatus();
        }

        private void guna2ImageButton1_Click(object sender, EventArgs e)
        {
            // Menampilkan pesan konfirmasi
            DialogResult result = MessageBox.Show("Are u sure Want Reboot rg?", "BOT [REHAN JEBOL]", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Merestart komputer
                Process.Start("shutdown", "/r /t 0");
            }
        }

        private void guna2ImageButton2_Click(object sender, EventArgs e)
        {
            // Menampilkan pesan konfirmasi
            DialogResult result = MessageBox.Show("Are u sure Want Shutdown rg?", "BOT [ANDHIKA]", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Melakukan shutdown komputer
                Process.Start("shutdown", "/s /f /t 0");
            }
        }


        private void guna2PictureBox3_Click(object sender, EventArgs e)
        {
            string url = "https://discord.gg/nGwqEzSF"; // Ganti dengan URL yang ingin Anda buka

            // Membuka URL dalam browser default
            Process.Start(url);
        }
    } 
    
}
