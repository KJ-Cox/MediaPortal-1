#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using MpeCore;
using MpeCore.Classes;
using MpeCore.Interfaces;
using MpeMaker.Dialogs;

namespace MpeMaker.Sections
{
  public partial class RequirementsSection : UserControl, ISectionControl
  {
    public PackageClass Package { get; set; }
    private DependencyItem SelectedItem { get; set; }
    private readonly MpeCore.Classes.VersionProvider.MediaPortalVersion MPDependency = new MpeCore.Classes.VersionProvider.MediaPortalVersion();

    public RequirementsSection()
    {
      InitializeComponent();
      SelectedItem = null;
      foreach (var versionProvider in MpeInstaller.VersionProviders)
      {
        ToolStripMenuItem testToolStripMenuItem = new ToolStripMenuItem();
        testToolStripMenuItem.Text = versionProvider.Value.DisplayName;
        testToolStripMenuItem.Tag = versionProvider.Value;
        testToolStripMenuItem.Click += testToolStripMenuItem_Click;
        mnu_add.DropDownItems.Add(testToolStripMenuItem);
        cmb_type.Items.Add(versionProvider.Value.DisplayName);
      }
      this.cmb_execution_condition.Items.AddRange(Enum.GetNames(typeof(ActionConditionEnum)));
    }

    private void testToolStripMenuItem_Click(object sender, EventArgs e)
    {
      ToolStripMenuItem menu = sender as ToolStripMenuItem;
      IVersionProvider type = menu.Tag as IVersionProvider;
      if (type != null)
      {
        DependencyItem item = new DependencyItem(type.DisplayName) { MinVersion = type.Version(null), MaxVersion = type.Version(null) };
        item.MaxVersion = new VersionInfo(); //don't specify max version
        Package.Dependencies.Add(item);
        list_versions.Items.Add(item);
      }
    }

    private void txt_id_TextChanged(object sender, EventArgs e)
    {
      if (SelectedItem == null)
        return;
      UpdateControlStates();
      SelectedItem.Type = cmb_type.Text;
      SelectedItem.WarnOnly = chk_warn.Checked;
      SelectedItem.Id = txt_id.Text;
      SelectedItem.Name = txt_name.Text;
      
      SelectedItem.MinVersion.Major = txt_version1_min.Text;
      SelectedItem.MinVersion.Minor = txt_version2_min.Text;
      SelectedItem.MinVersion.Build = txt_version3_min.Text;
      SelectedItem.MinVersion.Revision = txt_version4_min.Text;
      SelectedItem.MaxVersion.Major = txt_version1_max.Text;
      SelectedItem.MaxVersion.Minor = txt_version2_max.Text;
      SelectedItem.MaxVersion.Build = txt_version3_max.Text;
      SelectedItem.MaxVersion.Revision = txt_version4_max.Text;
      list_versions.SelectedItem = SelectedItem;

      //Refresh the message
      this.SelectedItem.Message = null;
      this.txt_message.Text = this.SelectedItem.Message; 


      if (MpeInstaller.VersionProviders.ContainsKey(cmb_type.Text))
        lbl_ver.Text = MpeInstaller.VersionProviders[cmb_type.Text].Version(txt_id.Text).ToString();

      SelectedItem.Condition = (ActionConditionEnum)this.cmb_execution_condition.SelectedIndex;

      this.refreshListBox();
    }

    private void UpdateControlStates()
    {
      bool isMPDep = cmb_type.Text == MPDependency.DisplayName;
      chk_warn.Enabled = !isMPDep;
      txt_id.Enabled = button1.Enabled = cmb_type.Text == new MpeCore.Classes.VersionProvider.ExtensionVersion().DisplayName;

      if (isMPDep)
      {
        chk_warn.Checked = false;
      }
    }

    public void Set(PackageClass pak)
    {
      Package = pak;
      list_versions.Items.Clear();
      foreach (DependencyItem item in Package.Dependencies.Items)
      {
        list_versions.Items.Add(item);
      }
      groupBox1.Enabled = false;
    }

    public PackageClass Get()
    {
      throw new NotImplementedException();
    }

    public void RefreshControl()
    {
      this.list_versions_SelectedIndexChanged(null, null);
      this.refreshListBox();
    }

    private void refreshListBox()
    {
      //Redraw the listbox
      this.list_versions.DrawMode = DrawMode.OwnerDrawFixed;
      this.list_versions.DrawMode = DrawMode.Normal;
    }

    private void list_versions_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (list_versions.SelectedItems.Count < 1)
      {
        groupBox1.Enabled = false;
        return;
      }
      groupBox1.Enabled = true;
      DependencyItem item = list_versions.SelectedItem as DependencyItem;
      SelectedItem = null;
      cmb_type.Text = item.Type;
      chk_warn.Checked = item.WarnOnly;
      UpdateControlStates();
      txt_id.Text = item.Id;
      txt_name.Text = item.Name;
      txt_message.Text = item.Message;
      txt_version1_min.Text = item.MinVersion.Major;
      txt_version2_min.Text = item.MinVersion.Minor;
      txt_version3_min.Text = item.MinVersion.Build;
      txt_version4_min.Text = item.MinVersion.Revision;
      txt_version1_max.Text = item.MaxVersion.Major;
      txt_version2_max.Text = item.MaxVersion.Minor;
      txt_version3_max.Text = item.MaxVersion.Build;
      txt_version4_max.Text = item.MaxVersion.Revision;
      SelectedItem = item;
      if (MpeInstaller.VersionProviders.ContainsKey(cmb_type.Text))
        lbl_ver.Text = MpeInstaller.VersionProviders[cmb_type.Text].Version(txt_id.Text).ToString();

      this.cmb_execution_condition.SelectedIndex = (int)SelectedItem.Condition;
      this.cmb_execution_condition.Enabled = SelectedItem.Type == "Extension";
    }

    private void BrowseInstalledExtensionIdsClick(object sender, EventArgs e)
    {
      var dlg = new InstalledExtensionsSelector();
      dlg.ShowDialog();
      if (dlg.Result == null) return;
      txt_id.Text = dlg.Result.GeneralInfo.Id;
      txt_name.Text = dlg.Result.GeneralInfo.Name;
    }

    private void mnu_del_Click(object sender, EventArgs e)
    {
      if (list_versions.SelectedItems.Count < 1)
        return;
      if (SelectedItem == null)
        return;
      if (MessageBox.Show("Do you want to remove dependency " + SelectedItem.Name, "", MessageBoxButtons.YesNo) !=
          DialogResult.Yes)
        return;
      Package.Dependencies.Items.Remove(SelectedItem);
      list_versions.Items.Remove(list_versions.SelectedItem);
    }

    private void txt_version1_min_KeyDown(object sender, KeyEventArgs e)
    {
      bool result = true;

      bool bTextHasAsterix = ((TextBox)sender).Text.Contains("*");

      bool numericKeys = (!bTextHasAsterix &&
                           (((e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) ||
                            (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9))
                           && e.Modifiers != Keys.Shift));

      bool ctrlA = e.KeyCode == Keys.A && e.Modifiers == Keys.Control;

      bool editKeys = (
                        (e.KeyCode == Keys.Z && e.Modifiers == Keys.Control) ||
                        (e.KeyCode == Keys.X && e.Modifiers == Keys.Control) ||
                        (e.KeyCode == Keys.C && e.Modifiers == Keys.Control) ||
                        (e.KeyCode == Keys.V && e.Modifiers == Keys.Control && !bTextHasAsterix) ||
                        e.KeyCode == Keys.Delete ||
                        e.KeyCode == Keys.Back);

      bool navigationKeys = (
                              e.KeyCode == Keys.Up ||
                              e.KeyCode == Keys.Right ||
                              e.KeyCode == Keys.Down ||
                              e.KeyCode == Keys.Left ||
                              e.KeyCode == Keys.Home ||
                              e.KeyCode == Keys.End);

      bool bIsAsterix = e.KeyCode == Keys.Multiply && ((TextBox)sender).Text.Length == 0;

      if (!(numericKeys || editKeys || navigationKeys || bIsAsterix))
      {
        result = false;
      }
      if (!result) // If not valid key then suppress and handle.
      {
        e.SuppressKeyPress = true;
        e.Handled = true;
      }
      else
        base.OnKeyDown(e);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
      if (e.KeyCode == Keys.Delete)
      {
        e.Handled = true;

        if (sender == list_versions)
          mnu_del_Click(null, null);
      }
    }
  }
}