#region License Information (Ms-PL)
// Copyright © 2016 Mikhail Beloshapkin
// All rights reserved.
//
// Author: Mikhail Beloshapkin mbeloshapkin@gmail.com
//
// Licensed under the Microsoft Public License (Ms-PL)
//
// Download the latest version https://github.com/mbeloshapkin/UnitMan
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;

using AeroGIS.Common;
using Converter.Properties;

namespace Converter {
    public partial class FrmConverter : Form {
        private UnitMan UM;
        private List<string> Domains;
        private bool IsInitializing;        

        public FrmConverter() {
            InitializeComponent();
            UM = new UnitMan();
            if (Settings.Default.LastFile.Length > 0) Load(Settings.Default.LastFile);
            btLoad.Focus();
        }

        private void Load(string AFileName) {
            IsInitializing = true;
            try {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(File.OpenRead(AFileName));
                UM.Load(xDoc);

                tbxUnitSystem.Text = UM.Description;

                // Make domains list                  
                Domains = UM.Domains; Domains.Sort();
                cbxDomains.DataSource = Domains;                

                cbxDomains.SelectedIndex = Domains.IndexOf(Settings.Default.LastDomain) >= 0 ?
                    Domains.IndexOf(Settings.Default.LastDomain) : 0;
                SelectDomain();
            }
            catch (Exception ex) {
                MessageBox.Show("UOM Manager failed to load from file " + AFileName +
                ". " + ex.Message,
                    "UOMs Definition file corrupted", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { IsInitializing = false; }
        }

        private void btLoad_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "UOM Definition file (*.xml)|*.xml";
            ofd.InitialDirectory = Settings.Default.LastFile.Length == 0 ?
                Application.StartupPath : Path.GetDirectoryName(Settings.Default.LastFile);
            if (ofd.ShowDialog() == DialogResult.OK) {
                Settings.Default.LastFile = ofd.FileName;
                Settings.Default.Save();
                Load(ofd.FileName);
            }
        }

        

        

        private string GetUOMDesc(string ASignature) {
            string desc = UM.NameOf(ASignature);
            string lb = UM.LabelOf(ASignature);
            return desc == lb? desc : desc + " (" + lb + ")"; /// Automatically generated UOMs could be of same name and label
        }

        private void SelectDomain() {
            string domain = (string)cbxDomains.SelectedItem;
            Settings.Default.LastDomain = domain; Settings.Default.Save();
            IEnumerable<string> uoms = UM.UOMsOf(domain);
            lbxFrom.Items.Clear(); lbxTo.Items.Clear();
            foreach (string uom in uoms) {
                TagUOM tagUOM = new TagUOM();
                tagUOM.Description = GetUOMDesc(uom);
                tagUOM.UOM = uom;
                lbxFrom.Items.Add(tagUOM); lbxTo.Items.Add(tagUOM);
            }
        }

        private void cbxDomains_SelectedIndexChanged(object sender, EventArgs e) {
            if (IsInitializing) return;
            SelectDomain();
        }        

        private struct TagUOM {
            public string Description, UOM;
            public override string ToString() { return Description; }
        }


        private void ComposeResult() {
            if (tbxVal.Text.Length == 0) { tbxVal.Text = "1"; return; }
            TagUOM from = (TagUOM)lbxFrom.SelectedItem; TagUOM to = (TagUOM)lbxTo.SelectedItem;
            double src = double.Parse(tbxVal.Text);
            double dst = UM.Convert(src, from.UOM, to.UOM);
            lbResult.Text = tbxVal.Text + " " +
                (src == 1 ? UM.NameOf(from.UOM) : UM.PluralOf(from.UOM)) +
                " = " + dst.ToString() + " " + (dst == 1 ? UM.NameOf(to.UOM) : UM.PluralOf(to.UOM));                
        }

        private void lbxFrom_SelectedIndexChanged(object sender, EventArgs e) {
            ComposeResult();
        }

        private void lbxTo_SelectedIndexChanged(object sender, EventArgs e) {
            ComposeResult();
        }

        private void tbxVal_TextChanged(object sender, EventArgs e) {
            ComposeResult();
        }

        #region Menu strip
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
            FrmAbout fa = new FrmAbout();
            fa.ShowDialog();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e) {
            btLoad_Click(sender, e);
        }
        #endregion
    }
}
