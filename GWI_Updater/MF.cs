using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using System.IO.Packaging;
using System.Net;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace GWI_Updater
{
    public partial class MF : Form
    {
        private static INI cfg = new INI(Directory.GetCurrentDirectory() + "\\GWI_Updater.ini");

        public MF()
        {
            InitializeComponent();
        }

        private void MF_Load(object sender, EventArgs e)
        {
            try
            {
                int width = int.Parse(cfg.Read("Application", "width"));
                int height = int.Parse(cfg.Read("Application", "height"));
                string bg_img = cfg.Read("Application", "bg_img");
                this.Size = new Size(width, height);
                this.BackgroundImage = Image.FromFile(bg_img);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void MF_Shown(object sender, EventArgs e)
        {
            GoUpdate();
        }

        private void MF_FormClosed(object sender, FormClosedEventArgs e)
        {
            Process.Start(cfg.Read("Application", "start_app"));
        }

        private void GoUpdate()
        {
            string patch_dir = cfg.Read("Update Info", "patch_dir");
            StreamWriter sw = new StreamWriter("versions.txt");
            BinaryWriter bw = new BinaryWriter(sw.BaseStream);

            WebClient wc = new WebClient();
            byte[] buffer = wc.DownloadData(patch_dir + "/versions.txt");

            bw.Write(buffer, 0, buffer.Length);
            bw.Close();

            List<Dictionary<string, string>> versions = new List<Dictionary<string, string>>();
            StreamReader sr = new StreamReader("versions.txt");
            string line;
            string version;
            string md5sum;
            Dictionary<string, string> vm;
            while (!sr.EndOfStream)
            {
                line = sr.ReadLine().Trim();
                if (line != "")
                {
                    version = line.Split('|')[0];
                    md5sum = line.Split('|')[1];
                    vm = new Dictionary<string, string>();
                    vm.Add("version", version);
                    vm.Add("md5sum", md5sum);
                    versions.Add(vm);
                }
            }
            sr.Close();

            while (versions.Count > 0)
            {
                if (versions[0]["version"] != cfg.Read("Update Info", "version"))
                {
                    versions.RemoveAt(0);
                }
                else
                {
                    versions.RemoveAt(0);
                    break;
                }
            }

            foreach (Dictionary<string, string> x in versions)
            {
                sw = new StreamWriter(cfg.Read("Update Info", "update_dir") + "//" + x["version"] + ".zip");
                bw = new BinaryWriter(sw.BaseStream);
                buffer = wc.DownloadData(patch_dir + "/" + x["version"] + ".zip");
                bw.Write(buffer, 0, buffer.Length);
                bw.Close();


                if (GetMD5Hash(cfg.Read("Update Info", "update_dir") + "//" + x["version"] + ".zip").ToLower() == x["md5sum"].ToLower())
                {
                    UnZipClass.UnZip(new string[] { cfg.Read("Update Info", "update_dir") + "//" + x["version"] + ".zip", cfg.Read("Update Info", "update_dir") });
                    File.Delete(cfg.Read("Update Info", "update_dir") + "//" + x["version"] + ".zip");
                    cfg.Write("Update Info", "version", x["version"]);
                }
                else
                {
                    File.Delete(cfg.Read("Update Info", "update_dir") + "//" + x["version"] + ".zip");
                    GoUpdate();
                }
            }

            this.Close();
        }

        //计算文件的MD5码
        private static string GetMD5Hash(string pathName)
        {
            string strResult = "";
            string strHashData = "";
            byte[] arrbytHashValue;
            System.IO.FileStream oFileStream = null;
            System.Security.Cryptography.MD5CryptoServiceProvider oMD5Hasher = new System.Security.Cryptography.MD5CryptoServiceProvider();
            try
            {

                oFileStream = new System.IO.FileStream(pathName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                arrbytHashValue = oMD5Hasher.ComputeHash(oFileStream);//计算指定Stream 对象的哈希值
                oFileStream.Close();

                //由以连字符分隔的十六进制对构成的String，其中每一对表示value 中对应的元素；例如“F-2C-4A”

                strHashData = System.BitConverter.ToString(arrbytHashValue);

                //替换-

                strHashData = strHashData.Replace("-", "");

                strResult = strHashData;

            }

            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return strResult;

        }
    }
}
