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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Ionic.Zip;
using Mediaportal.TV.Server.Common.Types.Enum;
using Mediaportal.TV.Server.Common.Types.Provider;
using Mediaportal.TV.Server.SetupControls;
using Mediaportal.TV.Server.SetupTV.Sections.Helpers;
using Mediaportal.TV.Server.TVControl.ServiceAgents;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;
using MediaPortal.Common.Utils.ExtensionMethods;

namespace Mediaportal.TV.Server.SetupTV.Sections
{
  public partial class Scanning : SectionSettings
  {
    private class FileDownloader : WebClient
    {
      public int TimeOut = 60000;

      public FileDownloader(int timeOutMilliseconds = 60000)
      {
        TimeOut = timeOutMilliseconds;
      }

      protected override WebRequest GetWebRequest(Uri address)
      {
        var result = base.GetWebRequest(address);
        if (result != null)
        {
          result.Timeout = TimeOut;
        }
        return result;
      }
    }

    private FileDownloader _downloader = null;
    private string _fileNameTuningDetails = null;

    public Scanning()
      : base("Scanning")
    {
      InitializeComponent();
    }

    private static bool CanReceiveUkSatellite(string countryName)
    {
      if (
        countryName != null &&
        (
          countryName.Equals("United Kingdom") ||
          // over-spill/unintended reception
          countryName.Equals("Belgium") ||
          countryName.Equals("France") ||
          countryName.Equals("Ireland") ||
          countryName.Equals("Netherlands, The")
        )
      )
      {
        return true;
      }
      return false;
    }

    public override void OnSectionActivated()
    {
      this.LogDebug("scanning: activating");

      // first activation
      string countryName = RegionInfo.CurrentRegion.EnglishName;
      bool canReceiveUkSatellite = CanReceiveUkSatellite(countryName);
      if (comboBoxProvidersProvider1Region.Enabled && comboBoxProvidersProvider1Region.Items.Count == 0)
      {
        if (countryName != null)
        {
          if (countryName.Equals("Australia"))
          {
            labelProvidersProvider1Region.Text = "Foxtel region:";
            comboBoxProvidersProvider1Region.DataSource = RegionOpenTvFoxtel.Values.ToList().OrderBy(r => r.ToString()).ToList();
            checkBoxAutomaticChannelGroupsProvider1Region.Text = checkBoxAutomaticChannelGroupsProvider1Region.Text.Replace("provider 1 region", "Foxtel region");
          }
          else if (countryName.Equals("New Zealand"))
          {
            labelProvidersProvider1Region.Text = "Sky region:";
            comboBoxProvidersProvider1Region.Items.AddRange(typeof(RegionOpenTvSkyNz).GetDescriptions());
            checkBoxAutomaticChannelGroupsProvider1Region.Text = checkBoxAutomaticChannelGroupsProvider1Region.Text.Replace("provider 1 region", "Foxtel region");
            labelProvidersProvider2Region.Text = "Freeview Satellite region:";
            comboBoxProvidersProvider2Region.Items.AddRange(typeof(BouquetFreeviewSatellite).GetDescriptions());
            checkBoxProvidersProvider2IsHighDefinition.Enabled = false;
            checkBoxAutomaticChannelGroupsProvider2Region.Text = checkBoxAutomaticChannelGroupsProvider2Region.Text.Replace("provider 2 region", "Freeview Satellite region");
          }
          else if (canReceiveUkSatellite)
          {
            labelProvidersProvider1Region.Text = "Sky UK region:";
            comboBoxProvidersProvider1Region.DataSource = RegionOpenTvSkyUk.Values.ToList().OrderBy(r => r.ToString()).ToList();
            checkBoxAutomaticChannelGroupsProvider1Region.Text = checkBoxAutomaticChannelGroupsProvider1Region.Text.Replace("provider 1 region", "Sky UK region");
            labelProvidersProvider2Region.Text = "Freesat region:";
            comboBoxProvidersProvider2Region.DataSource = RegionFreesat.Values.ToList().OrderBy(r => r.ToString()).ToList();
            checkBoxAutomaticChannelGroupsProvider2Region.Text = checkBoxAutomaticChannelGroupsProvider2Region.Text.Replace("provider 2 region", "Freesat region");
          }
          else if (countryName.Equals("United States"))
          {
            labelProvidersProvider1Region.Text = "Dish Network market:";
            comboBoxProvidersProvider1Region.DataSource = DishNetworkMarket.Values.ToList().OrderBy(m => m.ToString()).ToList();
            checkBoxProvidersProvider1IsHighDefinition.Enabled = false;
            checkBoxAutomaticChannelGroupsProvider1Region.Text = checkBoxAutomaticChannelGroupsProvider1Region.Text.Replace("provider 1 region", "Dish Network market");
          }
        }

        // Disable or hide unused fields.
        if (comboBoxProvidersProvider1Region.Items.Count == 0 && comboBoxProvidersProvider2Region.Items.Count == 0)
        {
          groupBoxProviders.Enabled = false;
          checkBoxAutomaticChannelGroupsProvider1Region.Visible = false;
          checkBoxAutomaticChannelGroupsProvider2Region.Visible = false;
        }
        else if (comboBoxProvidersProvider2Region.Items.Count == 0)
        {
          comboBoxProvidersProvider2Region.Enabled = false;
          checkBoxProvidersProvider2IsHighDefinition.Enabled = false;
          checkBoxProvidersPreferProvider2ChannelDetails.Enabled = false;
          checkBoxAutomaticChannelGroupsProvider2Region.Visible = false;
        }

        // Maximise the width of the region combo-boxes.
        int labelWidth = Math.Max(labelProvidersProvider1Region.Width, labelProvidersProvider2Region.Width);
        int newLeft = labelProvidersProvider1Region.Left + labelWidth + labelProvidersProvider1Region.Margin.Right + comboBoxProvidersProvider1Region.Margin.Left;
        int widthAdjustment = comboBoxProvidersProvider1Region.Left - newLeft;
        comboBoxProvidersProvider1Region.Left = newLeft;
        comboBoxProvidersProvider2Region.Left = newLeft;
        comboBoxProvidersProvider1Region.Width += widthAdjustment;
        comboBoxProvidersProvider2Region.Width += widthAdjustment;
      }

      // providers
      object selectedRegion;
      bool isHighDefinition;
      if (
        countryName != null &&
        (
          countryName.Equals("Australia") ||
          countryName.Equals("New Zealand") ||
          canReceiveUkSatellite
        )
      )
      {
        LoadOpenTvProvider(countryName, out selectedRegion, out isHighDefinition);
        if (selectedRegion != null)
        {
          comboBoxProvidersProvider1Region.SelectedItem = selectedRegion;
        }
        checkBoxProvidersProvider1IsHighDefinition.Checked = isHighDefinition;

        if (canReceiveUkSatellite)
        {
          LoadFreesatProvider(countryName, out selectedRegion, out isHighDefinition);
          if (selectedRegion != null)
          {
            comboBoxProvidersProvider2Region.SelectedItem = selectedRegion;
          }
          checkBoxProvidersProvider2IsHighDefinition.Checked = isHighDefinition;
        }
        else if (countryName.Equals("New Zealand"))
        {
          LoadFreeviewSatelliteProvider(out selectedRegion);
          if (selectedRegion != null)
          {
            comboBoxProvidersProvider2Region.SelectedItem = selectedRegion;
          }
        }
      }
      else if (string.Equals(countryName, "United States"))
      {
        LoadDishNetworkProvider(out selectedRegion);
        if (selectedRegion != null)
        {
          comboBoxProvidersProvider1Region.SelectedItem = selectedRegion;
        }
      }
      if (checkBoxProvidersPreferProvider2ChannelDetails.Enabled)
      {
        checkBoxProvidersPreferProvider2ChannelDetails.Checked = ServiceAgents.Instance.SettingServiceAgent.GetValue("scanPreferProvider2ChannelDetails", false);
      }

      // timing
      numericUpDownTimingMinimum.Value = ServiceAgents.Instance.SettingServiceAgent.GetValue("minimumScanTime", 2000);
      numericUpDownTimingLimitSingleTransmitter.Value = ServiceAgents.Instance.SettingServiceAgent.GetValue("timeLimitScanSingleTransmitter", 15000);
      numericUpDownTimingLimitNetworkInformation.Value = ServiceAgents.Instance.SettingServiceAgent.GetValue("timeLimitScanNetworkInformation", 15000);
      numericUpDownTimingLimitCableCard.Value = ServiceAgents.Instance.SettingServiceAgent.GetValue("timeLimitScanCableCard", 300000);

      // other
      checkBoxChannelMovementDetection.Checked = ServiceAgents.Instance.SettingServiceAgent.GetValue("scanEnableChannelMovementDetection", false);
      checkBoxPreferHighDefinitionChannelNumbers.Checked = ServiceAgents.Instance.SettingServiceAgent.GetValue("scanPreferHighDefinitionChannelNumbers", true);
      checkBoxSkipEncryptedChannels.Checked = !ServiceAgents.Instance.SettingServiceAgent.GetValue("scanStoreEncryptedChannels", true);

      // automatic channel groups
      ChannelGroupType channelGroupTypes = ChannelGroupType.FreesatChannelCategory | ChannelGroupType.NorDigChannelList | ChannelGroupType.VirginMediaChannelCategory;
      channelGroupTypes = (ChannelGroupType)ServiceAgents.Instance.SettingServiceAgent.GetValue("scanAutoCreateChannelGroups", (int)channelGroupTypes);
      checkBoxAutomaticChannelGroupsBroadcastStandards.Checked = channelGroupTypes.HasFlag(ChannelGroupType.BroadcastStandard);
      checkBoxAutomaticChannelGroupsSatellites.Checked = channelGroupTypes.HasFlag(ChannelGroupType.Satellite);
      checkBoxAutomaticChannelGroupsNorDigChannelLists.Checked = channelGroupTypes.HasFlag(ChannelGroupType.NorDigChannelList);
      checkBoxAutomaticChannelGroupsVirginMediaChannelCategories.Checked = channelGroupTypes.HasFlag(ChannelGroupType.VirginMediaChannelCategory);
      checkBoxAutomaticChannelGroupsFreesatChannelCategories.Checked = channelGroupTypes.HasFlag(ChannelGroupType.FreesatChannelCategory);

      checkBoxAutomaticChannelGroupsChannelProviders.Checked = channelGroupTypes.HasFlag(ChannelGroupType.ChannelProvider);
      string[] tempArray = ServiceAgents.Instance.SettingServiceAgent.GetValue("scanAutoCreateChannelGroupsChannelProviders", string.Empty).Split('|');
      textBoxAutomaticChannelGroupsChannelProviders.Text = string.Join(", ", tempArray);
      checkBoxAutomaticChannelGroupsDvbNetworks.Checked = channelGroupTypes.HasFlag(ChannelGroupType.DvbNetwork);
      tempArray = ServiceAgents.Instance.SettingServiceAgent.GetValue("scanAutoCreateChannelGroupsDvbNetworks", string.Empty).Split('|');
      textBoxAutomaticChannelGroupsDvbNetworks.Text = string.Join(", ", tempArray);
      checkBoxAutomaticChannelGroupsDvbBouquets.Checked = channelGroupTypes.HasFlag(ChannelGroupType.DvbBouquet);
      tempArray = ServiceAgents.Instance.SettingServiceAgent.GetValue("scanAutoCreateChannelGroupsDvbBouquets", string.Empty).Split('|');
      textBoxAutomaticChannelGroupsDvbBouquets.Text = string.Join(", ", tempArray);
      checkBoxAutomaticChannelGroupsDvbTargetRegions.Checked = channelGroupTypes.HasFlag(ChannelGroupType.DvbTargetRegion);
      tempArray = ServiceAgents.Instance.SettingServiceAgent.GetValue("scanAutoCreateChannelGroupsDvbTargetRegions", string.Empty).Split('|');
      textBoxAutomaticChannelGroupsDvbTargetRegions.Text = string.Join(", ", tempArray);

      if (checkBoxAutomaticChannelGroupsProvider1Region.Visible)
      {
        if (string.Equals(countryName, "United States"))
        {
          checkBoxAutomaticChannelGroupsProvider1Region.Checked = channelGroupTypes.HasFlag(ChannelGroupType.DishNetworkMarket);
        }
        else
        {
          checkBoxAutomaticChannelGroupsProvider1Region.Checked = channelGroupTypes.HasFlag(ChannelGroupType.OpenTvRegion);
        }
      }
      if (checkBoxAutomaticChannelGroupsProvider2Region.Visible)
      {
        if (string.Equals(countryName, "New Zealand"))
        {
          checkBoxAutomaticChannelGroupsProvider2Region.Checked = channelGroupTypes.HasFlag(ChannelGroupType.FreeviewSatellite);
        }
        else
        {
          checkBoxAutomaticChannelGroupsProvider2Region.Checked = channelGroupTypes.HasFlag(ChannelGroupType.FreesatRegion);
        }
      }

      DebugSettings();

      base.OnSectionActivated();
    }

    public override void OnSectionDeActivated()
    {
      this.LogDebug("scanning: deactivating");

      ChannelGroupType channelGroupTypes = ChannelGroupType.Manual;

      // providers
      string countryName = RegionInfo.CurrentRegion.EnglishName;
      bool canReceiveUkSatellite = CanReceiveUkSatellite(countryName);
      if (
        countryName != null &&
        (
          countryName.Equals("Australia") ||
          countryName.Equals("New Zealand") ||
          canReceiveUkSatellite
        )
      )
      {
        SaveOpenTvProvider(countryName, comboBoxProvidersProvider1Region.SelectedItem, checkBoxProvidersProvider1IsHighDefinition.Checked);
        if (checkBoxAutomaticChannelGroupsProvider1Region.Checked)
        {
          channelGroupTypes |= ChannelGroupType.OpenTvRegion;
        }

        if (canReceiveUkSatellite)
        {
          SaveFreesatProvider(comboBoxProvidersProvider2Region.SelectedItem, checkBoxProvidersProvider2IsHighDefinition.Checked);
          if (checkBoxAutomaticChannelGroupsProvider2Region.Checked)
          {
            channelGroupTypes |= ChannelGroupType.FreesatRegion;
          }
        }
        else if (countryName.Equals("New Zealand"))
        {
          ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanProviderFreeviewSatellite", (int)((BouquetFreeviewSatellite)comboBoxProvidersProvider2Region.SelectedItem));
          if (checkBoxAutomaticChannelGroupsProvider2Region.Checked)
          {
            channelGroupTypes |= ChannelGroupType.FreeviewSatellite;
          }
        }
      }
      else if (string.Equals(countryName, "United States"))
      {
        DishNetworkMarket market = (DishNetworkMarket)comboBoxProvidersProvider1Region.SelectedItem;
        if (market != null)
        {
          ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanProviderDishNetwork", string.Format("{0},{1}", market.Id, market.StateAbbreviation));
        }
        if (checkBoxAutomaticChannelGroupsProvider1Region.Checked)
        {
          channelGroupTypes |= ChannelGroupType.DishNetworkMarket;
        }
      }

      // timing
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("minimumScanTime", (int)numericUpDownTimingMinimum.Value);
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("timeLimitScanSingleTransmitter", (int)numericUpDownTimingLimitSingleTransmitter.Value);
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("timeLimitScanNetworkInformation", (int)numericUpDownTimingLimitNetworkInformation.Value);
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("timeLimitScanCableCard", (int)numericUpDownTimingLimitCableCard.Value);

      // other
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanEnableChannelMovementDetection", checkBoxChannelMovementDetection.Checked);
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanPreferHighDefinitionChannelNumbers", checkBoxPreferHighDefinitionChannelNumbers.Checked);
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanStoreEncryptedChannels", !checkBoxSkipEncryptedChannels.Checked);

      // automatic channel groups
      if (checkBoxAutomaticChannelGroupsChannelProviders.Checked)
      {
        channelGroupTypes |= ChannelGroupType.ChannelProvider;
      }
      if (checkBoxAutomaticChannelGroupsDvbNetworks.Checked)
      {
        channelGroupTypes |= ChannelGroupType.DvbNetwork;
      }
      if (checkBoxAutomaticChannelGroupsDvbBouquets.Checked)
      {
        channelGroupTypes |= ChannelGroupType.DvbBouquet;
      }
      if (checkBoxAutomaticChannelGroupsDvbTargetRegions.Checked)
      {
        channelGroupTypes |= ChannelGroupType.DvbTargetRegion;
      }
      if (checkBoxAutomaticChannelGroupsBroadcastStandards.Checked)
      {
        channelGroupTypes |= ChannelGroupType.BroadcastStandard;
      }
      if (checkBoxAutomaticChannelGroupsSatellites.Checked)
      {
        channelGroupTypes |= ChannelGroupType.Satellite;
      }
      if (checkBoxAutomaticChannelGroupsFreesatChannelCategories.Checked)
      {
        channelGroupTypes |= ChannelGroupType.FreesatChannelCategory;
      }
      if (checkBoxAutomaticChannelGroupsNorDigChannelLists.Checked)
      {
        channelGroupTypes |= ChannelGroupType.NorDigChannelList;
      }
      if (checkBoxAutomaticChannelGroupsVirginMediaChannelCategories.Checked)
      {
        channelGroupTypes |= ChannelGroupType.VirginMediaChannelCategory;
      }
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanAutoCreateChannelGroups", (int)channelGroupTypes);

      HashSet<string> items = new HashSet<string>(Regex.Split(textBoxAutomaticChannelGroupsChannelProviders.Text.Trim(), @"\s*,\s*"));
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanAutoCreateChannelGroupsChannelProviders", string.Join("|", items));
      items = new HashSet<string>(Regex.Split(textBoxAutomaticChannelGroupsDvbNetworks.Text.Trim(), @"\s*,\s*"));
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanAutoCreateChannelGroupsDvbNetworks", string.Join("|", items));
      items = new HashSet<string>(Regex.Split(textBoxAutomaticChannelGroupsDvbBouquets.Text.Trim(), @"\s*,\s*"));
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanAutoCreateChannelGroupsDvbBouquets", string.Join("|", items));
      items = new HashSet<string>(Regex.Split(textBoxAutomaticChannelGroupsDvbTargetRegions.Text.Trim(), @"\s*,\s*"));
      ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanAutoCreateChannelGroupsDvbTargetRegions", string.Join("|", items));

      DebugSettings();

      // TODO trigger server-side config reloading for provider and time limit config... or should they just be re-read automatically at the start of each scan???

      base.OnSectionDeActivated();
    }

    private void DebugSettings()
    {
      this.LogDebug("scanning: settings...");
      this.LogDebug("  providers...");
      this.LogDebug("    country                 = {0}", RegionInfo.CurrentRegion.EnglishName);
      if (comboBoxProvidersProvider1Region.Enabled)
      {
        this.LogDebug("    provider 1...");
        this.LogDebug("      region                = {0}", comboBoxProvidersProvider1Region.SelectedItem);
        this.LogDebug("      high definition?      = {0}", checkBoxProvidersProvider1IsHighDefinition.Checked);
        this.LogDebug("      region group?         = {0}", checkBoxAutomaticChannelGroupsProvider1Region.Checked);
        if (comboBoxProvidersProvider2Region.Enabled)
        {
          this.LogDebug("    provider 2...");
          this.LogDebug("      region                = {0}", comboBoxProvidersProvider2Region.SelectedItem);
          this.LogDebug("      high definition?      = {0}", checkBoxProvidersProvider2IsHighDefinition.Checked);
          this.LogDebug("      region group?         = {0}", checkBoxAutomaticChannelGroupsProvider2Region.Checked);
          this.LogDebug("    prefer provider 2?      = {0}", checkBoxProvidersPreferProvider2ChannelDetails.Checked);
        }
      }
      this.LogDebug("  timing...");
      this.LogDebug("    minimum                 = {0} ms", numericUpDownTimingMinimum.Value);
      this.LogDebug("    single transmitter      = {0} ms", numericUpDownTimingLimitSingleTransmitter.Value);
      this.LogDebug("    network information     = {0} ms", numericUpDownTimingLimitNetworkInformation.Value);
      this.LogDebug("    CableCARD               = {0} ms", numericUpDownTimingLimitCableCard.Value);
      this.LogDebug("  detect channel movement?  = {0}", checkBoxChannelMovementDetection.Checked);
      this.LogDebug("  prefer HD LCNs?           = {0}", checkBoxPreferHighDefinitionChannelNumbers.Checked);
      this.LogDebug("  store encrypted channels? = {0}", !checkBoxSkipEncryptedChannels.Checked);
      this.LogDebug("  automatic channel groups...");
      this.LogDebug("    providers               = {0} [{1}]", checkBoxAutomaticChannelGroupsChannelProviders.Checked, textBoxAutomaticChannelGroupsChannelProviders.Text);
      this.LogDebug("    DVB networks            = {0} [{1}]", checkBoxAutomaticChannelGroupsDvbNetworks.Checked, textBoxAutomaticChannelGroupsDvbNetworks.Text);
      this.LogDebug("    DVB bouquets            = {0} [{1}]", checkBoxAutomaticChannelGroupsDvbBouquets.Checked, textBoxAutomaticChannelGroupsDvbBouquets.Text);
      this.LogDebug("    DVB target regions      = {0} [{1}]", checkBoxAutomaticChannelGroupsDvbTargetRegions.Checked, textBoxAutomaticChannelGroupsDvbTargetRegions.Text);
      this.LogDebug("    broadcast standards     = {0}", checkBoxAutomaticChannelGroupsBroadcastStandards.Checked);
      this.LogDebug("    satellites              = {0}", checkBoxAutomaticChannelGroupsSatellites.Checked);
      this.LogDebug("    Freesat categories      = {0}", checkBoxAutomaticChannelGroupsFreesatChannelCategories.Checked);
      this.LogDebug("    NorDig channel lists    = {0}", checkBoxAutomaticChannelGroupsNorDigChannelLists.Checked);
      this.LogDebug("    Virgin Media categories = {0}", checkBoxAutomaticChannelGroupsVirginMediaChannelCategories.Checked);
    }

    private void LoadOpenTvProvider(string countryName, out object selectedRegion, out bool isHighDefinition)
    {
      selectedRegion = null;
      isHighDefinition = false;

      int defaultBouquetId = 0;
      int defaultRegionId = 0;
      if (string.Equals(countryName, "Australia"))
      {
        // Assume most people have DVB-S2 tuners and can receive HD channels.
        defaultBouquetId = (int)BouquetOpenTvFoxtel.HdResidential;

        // For the default region, choose the satellite network because it has
        // the most subscribers and Sydney because it is the most populous.
        RegionOpenTvFoxtel defaultRegion = RegionOpenTvFoxtel.Satellite_Sydney;

        // The PC time-zone can give us more location detail. Refer to the
        // internet for information about Australian time-zones. The zone names
        // below were determined by changing Windows time-zone on a machine
        // running XP/.NET 4 in October 2015.
        switch (TimeZone.CurrentTimeZone.StandardName)
        {
          case "E. Australia Standard Time":    // Queensland (+10) - Brisbane, Gold Coast
            defaultRegion = RegionOpenTvFoxtel.Satellite_Brisbane;  // Brisbane is more populous than Gold Coast
            break;
          case "AUS Eastern Standard Time":     // ACT, New South Wales, Victoria (+10/11) - Canberra, Central Coast, Sydney, Melbourne
            defaultRegion = RegionOpenTvFoxtel.Satellite_Sydney;    // Sydney is most populous
            break;
          case "Cen. Australia Standard Time":  // South Australia (+9.5/10.5) - Adelaide
            defaultRegion = RegionOpenTvFoxtel.Satellite_Adelaide;
            break;
          case "AUS Central Standard Time":     // Northern Territory (+9.5) - Darwin
            // (region codes currently unknown)
            break;
          case "W. Australia Standard Time":    // Western Australia (+8) - Perth
            defaultRegion = RegionOpenTvFoxtel.Satellite_Perth;
            break;
          case "Tasmania Standard Time":        // Tasmania (+10/11) - Hobart
            // (region codes currently unknown)
            break;
        }

        defaultRegionId = defaultRegion.Id;
      }
      else if (string.Equals(countryName, "New Zealand"))
      {
        // Assume most people have DVB-S2 tuners and the HD ticket so can
        // receive Sky HD.
        defaultBouquetId = (int)BouquetOpenTvSkyNz.Ca2HdHd;

        // For the default region, choose the most populous city.
        defaultRegionId = (int)RegionOpenTvSkyNz.Auckland;
      }
      else
      {
        // AFAIK most Sky HD channels can no longer be decrypted with a PC, so
        // default to SD.
        defaultBouquetId = (int)BouquetOpenTvSkyUk.BSkybBouquet1_DthEngland;

        // For the default region, choose the most populous city. 
        defaultRegionId = RegionOpenTvSkyUk.London;
      }

      string defaultConfig = string.Format("{0},{1}", defaultBouquetId, defaultRegionId);
      string config = ServiceAgents.Instance.SettingServiceAgent.GetValue("scanProviderOpenTv", defaultConfig);
      if (string.IsNullOrEmpty(config))
      {
        config = defaultConfig;
      }
      string[] parts = config.Split(',');
      if (parts == null || parts.Length != 2)
      {
        parts = defaultConfig.Split(',');
      }
      int bouquetId;
      int regionId;
      int.TryParse(parts[0], out bouquetId);
      int.TryParse(parts[1], out regionId);

      if (countryName.Equals("Australia"))
      {
        if (
          bouquetId == (int)BouquetOpenTvFoxtel.HdCommercialAndPublic ||
          bouquetId == (int)BouquetOpenTvFoxtel.HdCourtesyAndVips ||
          bouquetId == (int)BouquetOpenTvFoxtel.HdMduLite ||
          bouquetId == (int)BouquetOpenTvFoxtel.HdOptus ||
          bouquetId == (int)BouquetOpenTvFoxtel.HdResidential ||
          bouquetId == (int)BouquetOpenTvFoxtel.HdSports
        )
        {
          isHighDefinition = true;
        }

        selectedRegion = RegionOpenTvFoxtel.GetValue(regionId, (BouquetOpenTvFoxtel)bouquetId);
      }
      else if (countryName.Equals("New Zealand"))
      {
        isHighDefinition = bouquetId == (int)BouquetOpenTvSkyNz.Ca2HdHd;
        selectedRegion = ((RegionOpenTvSkyNz)regionId).GetDescription();
      }
      else
      {
        if (
          bouquetId == (int)BouquetOpenTvSkyUk.BSkybBouquet5_HdEngland ||
          bouquetId == (int)BouquetOpenTvSkyUk.BSkybBouquet6_HdScotland ||
          bouquetId == (int)BouquetOpenTvSkyUk.BSkybBouquet7_HdWales ||
          bouquetId == (int)BouquetOpenTvSkyUk.BSkybBouquet8_HdOther
        )
        {
          isHighDefinition = true;
        }

        selectedRegion = (RegionOpenTvSkyUk)regionId;
      }
    }

    private void LoadFreesatProvider(string countryName, out object selectedRegion, out bool isHighDefinition)
    {
      selectedRegion = null;
      isHighDefinition = false;

      string defaultConfig;
      if (countryName.Equals("Ireland"))
      {
        defaultConfig = string.Format("{0},{1}", (int)BouquetFreesat.NorthernIrelandHd, RegionFreesat.NorthernIreland.Id);
      }
      else
      {
        defaultConfig = string.Format("{0},{1}", (int)BouquetFreesat.EnglandHd, RegionFreesat.London_London.Id);
      }
      string config = ServiceAgents.Instance.SettingServiceAgent.GetValue("scanProviderFreesat", defaultConfig);
      if (string.IsNullOrEmpty(config))
      {
        config = defaultConfig;
      }
      string[] parts = config.Split(',');
      if (parts == null || parts.Length != 2)
      {
        parts = defaultConfig.Split(',');
      }
      int bouquetId;
      int regionId;
      int.TryParse(parts[0], out bouquetId);
      int.TryParse(parts[1], out regionId);

      if (
        bouquetId == (int)BouquetFreesat.EnglandG2 ||
        bouquetId == (int)BouquetFreesat.EnglandHd ||
        bouquetId == (int)BouquetFreesat.NorthernIrelandG2 ||
        bouquetId == (int)BouquetFreesat.NorthernIrelandHd ||
        bouquetId == (int)BouquetFreesat.ScotlandG2 ||
        bouquetId == (int)BouquetFreesat.ScotlandHd ||
        bouquetId == (int)BouquetFreesat.WalesG2 ||
        bouquetId == (int)BouquetFreesat.WalesHd
      )
      {
        isHighDefinition = true;
      }

      selectedRegion = (RegionFreesat)regionId;
    }

    private void LoadFreeviewSatelliteProvider(out object selectedRegion)
    {
      int bouquetId = ServiceAgents.Instance.SettingServiceAgent.GetValue("scanProviderFreeviewSatellite", (int)BouquetFreeviewSatellite.Auckland);
      if (bouquetId == 0)
      {
        selectedRegion = BouquetFreeviewSatellite.Auckland;
        return;
      }

      selectedRegion = (BouquetFreeviewSatellite)bouquetId;
    }

    private void LoadDishNetworkProvider(out object selectedMarket)
    {
      selectedMarket = null;

      // For the default market, choose New York because it is the most
      // populous city in the US.
      DishNetworkMarket defaultMarket = DishNetworkMarket.NewYork;

      // The PC time-zone can give us more location detail. Refer to the
      // internet for information about American time-zones. The zone names
      // below were determined by changing Windows time-zone on a machine
      // running XP/.NET 4 in October 2015.
      switch (TimeZone.CurrentTimeZone.StandardName)
      {
        case "Hawaiian Standard Time":    // Hawaii (-10/9)
          defaultMarket = DishNetworkMarket.Honolulu;
          break;
        case "Alaskan Standard Time":     // Alaska (-9/8)
          defaultMarket = DishNetworkMarket.Anchorage;
          break;
        case "Pacific Standard Time":     // Pacific Time (-8/7)
          defaultMarket = DishNetworkMarket.LosAngeles;
          break;
        case "Mountain Standard Time":    // Mountain Time (-7/6)
          defaultMarket = DishNetworkMarket.ElPaso;   // ...or Denver
          break;
        case "US Mountain Standard Time": // Mountain Standard Time (-7) - Arizona
          defaultMarket = DishNetworkMarket.Phoenix;
          break;
        case "Central Standard Time":     // Central Time (-6/5)
          defaultMarket = DishNetworkMarket.Chicago;
          break;
        case "Eastern Standard Time":     // Eastern Time (-5/4)
          defaultMarket = DishNetworkMarket.NewYork;
          break;
      }

      string defaultConfig = string.Format("{0},{1}", defaultMarket.Id, defaultMarket.StateAbbreviation);
      string config = ServiceAgents.Instance.SettingServiceAgent.GetValue("scanProviderDishNetwork", defaultConfig);
      if (string.IsNullOrEmpty(config))
      {
        config = defaultConfig;
      }
      string[] parts = config.Split(',');
      if (parts == null || parts.Length != 2)
      {
        parts = defaultConfig.Split(',');
      }
      int marketId;
      int.TryParse(parts[0], out marketId);

      selectedMarket = DishNetworkMarket.GetValue(marketId, parts[1]);
    }

    private void SaveOpenTvProvider(string countryName, object selectedRegion, bool isHighDefinition)
    {
      int bouquetId = 0;
      int regionId = 0;
      if (countryName.Equals("Australia"))
      {
        RegionOpenTvFoxtel region = (RegionOpenTvFoxtel)selectedRegion;
        if (region != null)
        {
          regionId = region.Id;
          bouquetId = (int)region.Bouquet;
          if (isHighDefinition)
          {
            if (region.Bouquet == BouquetOpenTvFoxtel.Optus)
            {
              bouquetId = (int)BouquetOpenTvFoxtel.HdOptus;
            }
            else
            {
              bouquetId = (int)BouquetOpenTvFoxtel.HdResidential;
            }
          }
        }
      }
      else if (countryName.Equals("New Zealand"))
      {
        if (isHighDefinition)
        {
          bouquetId = (int)BouquetOpenTvSkyNz.Ca2HdHd;
        }
        else
        {
          bouquetId = (int)BouquetOpenTvSkyNz.Ca2Sd;
        }
        regionId = (int)((RegionOpenTvSkyNz)selectedRegion);
      }
      else
      {
        RegionOpenTvSkyUk region = (RegionOpenTvSkyUk)selectedRegion;
        if (region != null)
        {
          regionId = region.Id;
          if (
            region == RegionOpenTvSkyUk.NorthernIreland ||
            region == RegionOpenTvSkyUk.ChannelIsles ||
            region == RegionOpenTvSkyUk.RepublicOfIreland
          )
          {
            if (isHighDefinition)
            {
              bouquetId = (int)BouquetOpenTvSkyUk.BSkybBouquet8_HdOther;
            }
            else
            {
              bouquetId = (int)BouquetOpenTvSkyUk.BSkybBouquet4_DthOther;
            }
          }
          else if (string.Equals(region.Country, "Scotland"))
          {
            if (isHighDefinition)
            {
              bouquetId = (int)BouquetOpenTvSkyUk.BSkybBouquet6_HdScotland;
            }
            else
            {
              bouquetId = (int)BouquetOpenTvSkyUk.BSkybBouquet2_DthScotland;
            }
          }
          else if (region.Id <= RegionOpenTvSkyUk.Wales.Id)
          {
            if (isHighDefinition)
            {
              bouquetId = (int)BouquetOpenTvSkyUk.BSkybBouquet5_HdEngland;
            }
            else
            {
              bouquetId = (int)BouquetOpenTvSkyUk.BSkybBouquet1_DthEngland;
            }
          }
          else
          {
            if (isHighDefinition)
            {
              bouquetId = (int)BouquetOpenTvSkyUk.BSkybBouquet7_HdWales;
            }
            else
            {
              bouquetId = (int)BouquetOpenTvSkyUk.BSkybBouquet3_DthWales;
            }
          }
        }
      }

      ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanProviderOpenTv", string.Format("{0},{1}", bouquetId, regionId));
    }

    private void SaveFreesatProvider(object selectedRegion, bool isHighDefinition)
    {
      RegionFreesat region = (RegionFreesat)selectedRegion;
      BouquetFreesat bouquet;
      if (string.Equals(region.Country, "Scotland"))
      {
        if (isHighDefinition)
        {
          bouquet = BouquetFreesat.ScotlandHd;
        }
        else
        {
          bouquet = BouquetFreesat.ScotlandSd;
        }
      }
      else if (string.Equals(region.Country, "Wales"))
      {
        if (isHighDefinition)
        {
          bouquet = BouquetFreesat.WalesHd;
        }
        else
        {
          bouquet = BouquetFreesat.WalesSd;
        }
      }
      else if (string.Equals(region.Country, "Northern Ireland"))
      {
        if (isHighDefinition)
        {
          bouquet = BouquetFreesat.NorthernIrelandHd;
        }
        else
        {
          bouquet = BouquetFreesat.NorthernIrelandSd;
        }
      }
      else
      {
        if (isHighDefinition)
        {
          bouquet = BouquetFreesat.EnglandHd;
        }
        else
        {
          bouquet = BouquetFreesat.EnglandSd;
        }
      }

      ServiceAgents.Instance.SettingServiceAgent.SaveValue("scanProviderFreesat", string.Format("{0},{1}", (int)bouquet, region.Id));
    }

    #region tuning detail update

    private void buttonUpdateTuningDetails_Click(object sender, EventArgs e)
    {
      if (_downloader != null)
      {
        this.LogDebug("scanning: cancel tuning detail update");
        buttonUpdateTuningDetails.Text = "Cancelling...";
        _downloader.CancelAsync();
        return;
      }

      this.LogDebug("scanning: backup tuning details");
      string tuningDetailRootPath = TuningDetailFilter.GetDataPath();
      string fileNameBackupTemp = Path.Combine(tuningDetailRootPath, "backup_new.zip");
      try
      {
        ZipFile zipFile = new ZipFile(fileNameBackupTemp, System.Text.Encoding.UTF8);
        try
        {
          foreach (string directory in Directory.GetDirectories(tuningDetailRootPath))
          {
            zipFile.AddDirectory(directory, Path.GetFileName(directory));
          }
          zipFile.Save();
        }
        finally
        {
          zipFile.Dispose();
        }

        string fileNameBackupFinal = Path.Combine(tuningDetailRootPath, "backup.zip");
        if (File.Exists(fileNameBackupFinal))
        {
          File.Delete(fileNameBackupFinal);
        }
        File.Move(fileNameBackupTemp, fileNameBackupFinal);
      }
      catch (Exception ex)
      {
        this.LogError(ex, "scanning: failed to backup tuning details before update");
        MessageBox.Show(string.Format("Backup failed. {0}", SENTENCE_CHECK_LOG_FILES), MESSAGE_CAPTION, MessageBoxButtons.OK);
        return;
      }

      this.LogDebug("scanning: tuning detail backup successful, starting download");
      buttonUpdateTuningDetails.Text = "Cancel...";
      _fileNameTuningDetails = Path.Combine(tuningDetailRootPath, "tuning_details.zip");
      using (_downloader = new FileDownloader(120000))
      {
        _downloader.Proxy.Credentials = CredentialCache.DefaultCredentials;
        _downloader.DownloadFileCompleted += OnTuningDetailDownloadCompleted;
        _downloader.DownloadFileAsync(new Uri("http://install.team-mediaportal.com/tvsetup/TVE_3.5/tuning_details.zip"), _fileNameTuningDetails);
      }
    }

    private void OnTuningDetailDownloadCompleted(object sender, AsyncCompletedEventArgs e)
    {
      if (e.Cancelled)
      {
        this.LogDebug("scanning: tuning detail download cancelled");
      }
      else if (e.Error != null)
      {
        this.LogError(e.Error, "scanning: failed to download tuning details");
        MessageBox.Show(string.Format("Download failed. {0}", SENTENCE_CHECK_LOG_FILES), MESSAGE_CAPTION, MessageBoxButtons.OK);
      }
      else
      {
        this.LogDebug("scanning: tuning detail download successful, extracting");
        try
        {
          foreach (string directory in Directory.GetDirectories(Path.GetDirectoryName(_fileNameTuningDetails)))
          {
            Directory.Delete(directory, true);
          }

          ZipFile zipFile = new ZipFile(_fileNameTuningDetails, System.Text.Encoding.UTF8);
          try
          {
            zipFile.ExtractAll(Path.GetDirectoryName(_fileNameTuningDetails), ExtractExistingFileAction.OverwriteSilently);
          }
          finally
          {
            zipFile.Dispose();
          }
          this.LogDebug("scanning: tuning details update successful");
        }
        catch (Exception ex)
        {
          this.LogError(ex, "scanning: failed to extract and update tuning details");
          MessageBox.Show(string.Format("Extract and update failed. {0}", SENTENCE_CHECK_LOG_FILES), MESSAGE_CAPTION, MessageBoxButtons.OK);
        }
      }

      _downloader = null;
      buttonUpdateTuningDetails.Text = "Update tuning details";
    }

    #endregion

    private void checkBoxAutomaticChannelGroupsChannelProviders_CheckedChanged(object sender, EventArgs e)
    {
      textBoxAutomaticChannelGroupsChannelProviders.Enabled = checkBoxAutomaticChannelGroupsChannelProviders.Checked;
    }

    private void checkBoxAutomaticChannelGroupsDvbNetworks_CheckedChanged(object sender, EventArgs e)
    {
      textBoxAutomaticChannelGroupsDvbNetworks.Enabled = checkBoxAutomaticChannelGroupsDvbNetworks.Checked;
    }

    private void checkBoxAutomaticChannelGroupsDvbBouquets_CheckedChanged(object sender, EventArgs e)
    {
      textBoxAutomaticChannelGroupsDvbBouquets.Enabled = checkBoxAutomaticChannelGroupsDvbBouquets.Checked;
    }

    private void checkBoxAutomaticChannelGroupsDvbTargetRegions_CheckedChanged(object sender, EventArgs e)
    {
      textBoxAutomaticChannelGroupsDvbTargetRegions.Enabled = checkBoxAutomaticChannelGroupsDvbTargetRegions.Checked;
    }

    private void numericUpDownTimingMinimum_ValueChanged(object sender, EventArgs e)
    {
      if (numericUpDownTimingLimitSingleTransmitter.Value < numericUpDownTimingMinimum.Value)
      {
        numericUpDownTimingLimitSingleTransmitter.Value = numericUpDownTimingMinimum.Value;
      }
      if (numericUpDownTimingLimitNetworkInformation.Value < numericUpDownTimingMinimum.Value)
      {
        numericUpDownTimingLimitNetworkInformation.Value = numericUpDownTimingMinimum.Value;
      }
      if (numericUpDownTimingLimitCableCard.Value < numericUpDownTimingMinimum.Value)
      {
        numericUpDownTimingLimitCableCard.Value = numericUpDownTimingMinimum.Value;
      }
    }

    private void numericUpDownTimingLimitSingleTransmitter_ValueChanged(object sender, EventArgs e)
    {
      if (numericUpDownTimingLimitSingleTransmitter.Value < numericUpDownTimingMinimum.Value)
      {
        numericUpDownTimingMinimum.Value = numericUpDownTimingLimitSingleTransmitter.Value;
      }
    }

    private void numericUpDownTimingLimitNetworkInformation_ValueChanged(object sender, EventArgs e)
    {
      if (numericUpDownTimingLimitNetworkInformation.Value < numericUpDownTimingMinimum.Value)
      {
        numericUpDownTimingMinimum.Value = numericUpDownTimingLimitNetworkInformation.Value;
      }
    }

    private void numericUpDownTimingLimitCableCard_ValueChanged(object sender, EventArgs e)
    {
      if (numericUpDownTimingLimitCableCard.Value < numericUpDownTimingMinimum.Value)
      {
        numericUpDownTimingMinimum.Value = numericUpDownTimingLimitCableCard.Value;
      }
    }
  }
}