namespace Sitecore.Support.Shell.Applications.Analytics.Personalization
{
  using Sitecore;
  using Sitecore.Analytics.Data;
  using Sitecore.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Marketing.Client.Shims;
  using Sitecore.Shell.Applications.Analytics.Personalization;
  using Sitecore.Support.Marketing.Client.Shims;
  using Sitecore.Text;
  using Sitecore.Web;
  using Sitecore.Web.UI.HtmlControls;
  using Sitecore.Web.UI.Sheer;
  using Sitecore.Web.UI.WebControls;
  using Sitecore.Web.UI.XamlSharp.Xaml;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using System.Runtime.InteropServices;
  using System.Text;
  using System.Web;
  using System.Web.UI;
  using System.Web.UI.HtmlControls;

  public class SelectMultiplePresets : BaseSelectPresetDialog
  {
    protected bool AddPreset(Item presetItem)
    {
      Assert.ArgumentNotNull(presetItem, "presetItem");
      if (this.SelectedPresetIds.ToLowerInvariant().Contains(presetItem.ID.ToString().ToLowerInvariant()))
      {
        return false;
      }
      this.SelectedPresetIds = this.SelectedPresetIds + "|" + presetItem.ID;
      return true;
    }

    private Dictionary<string, float> CalculateData(ContentProfile profile, Dictionary<string, List<PresetKeyValue>> keyValues, Dictionary<string, float> presetPercentages)
    {
      Assert.ArgumentNotNull(profile, "profile");
      Assert.ArgumentNotNull(keyValues, "keyValues");
      Assert.ArgumentNotNull(presetPercentages, "presetPercentages");
      Dictionary<string, float> result = profile.Keys.ToDictionary<ContentProfileKeyData, string, float>(key => key.Key, key => 0f);
      List<string> list = new List<string>(keyValues.Keys);
      foreach (string key in list)
      {
        Func<ContentProfileKeyData, bool> predicate = null;
        float defaultValue = 0f;
        if (keyValues[key].Count > 0)
        {
          foreach (PresetKeyValue value2 in keyValues[key])
          {
            float keyValue = value2.KeyValue;
            if (!base.IsCustomData && this.AllowPresetWeight)
            {
              float num3 = 0f;
              if (presetPercentages.ContainsKey(value2.PresetId))
              {
                num3 = presetPercentages[value2.PresetId];
              }
              keyValue = (keyValue * num3) / 100f;
            }
            defaultValue += keyValue;
          }
          if (!base.IsCustomData && !this.AllowPresetWeight)
          {
            defaultValue /= (float)keyValues[key].Count;
          }
        }
        else
        {
          if (predicate == null)
          {
            predicate = k => string.Compare(k.Name, key, StringComparison.InvariantCultureIgnoreCase) == 0;
          }
          ContentProfileKeyData data = profile.Keys.FirstOrDefault<ContentProfileKeyData>(predicate);
          if (data != null)
          {
            defaultValue = data.DefaultValue;
          }
        }
        if (!result.ContainsKey(key))
        {
          result.Add(key, 0f);
        }
        result[key] = defaultValue;
      }
      return Assert.ResultNotNull<Dictionary<string, float>>(result);
    }

    private void CollectData(Item profileItem, ContentProfile profile, out Dictionary<string, List<PresetKeyValue>> keyValues, out Dictionary<string, float> presetPercentages)
    {
      Func<ContentProfile, bool> predicate = null;
      Assert.ArgumentNotNull(profileItem, "profileItem");
      Assert.ArgumentNotNull(profile, "profile");
      keyValues = profile.Keys.ToDictionary<ContentProfileKeyData, string, List<PresetKeyValue>>(key => key.Key, key => new List<PresetKeyValue>());
      presetPercentages = new Dictionary<string, float>();
      if (base.IsCustomData)
      {
        Dictionary<string, string> sliderIds = base.CustomizationPresetCard.SliderIds;
        foreach (string str in sliderIds.Keys)
        {
          float num;
          string str2 = str.Replace("_", "$");
          string s = this.Page.Request[str2];
          if (float.TryParse(s, out num))
          {
            Assert.IsNotNull(num, "value for " + str2);
            string strB = sliderIds[str];
            ContentProfileKeyData data = null;
            foreach (ContentProfileKeyData data2 in profile.Keys)
            {
              if (string.Compare(data2.Name, strB, StringComparison.InvariantCulture) == 0)
              {
                data = data2;
                break;
              }
            }
            Assert.IsNotNull(data, "key data");
            if (!keyValues.ContainsKey(data.Key))
            {
              keyValues.Add(data.Key, new List<PresetKeyValue>());
            }
            PresetKeyValue item = new PresetKeyValue
            {
              PresetId = "[None]",
              KeyValue = num
            };
            keyValues[data.Key].Add(item);
          }
        }
      }
      else
      {
        Item[] selectedPresets = this.GetSelectedPresets();
        float result = (selectedPresets.Length > 0) ? (100f / ((float)selectedPresets.Length)) : 0f;
        foreach (Item item in selectedPresets)
        {
          if (this.AllowPresetWeight)
          {
            string str5 = (this.PresetToControlIdMap[item.ID.ToShortID().ToString().ToUpperInvariant()] + "$Combobox").Replace("_", "$");
            string str6 = this.Page.Request[str5] ?? string.Empty;
            if (!float.TryParse(str6.TrimEnd(new char[] { '%' }), out result))
            {
              result = 0f;
            }
          }
          if (!presetPercentages.ContainsKey(item.Key))
          {
            presetPercentages.Add(item.Key, result);
          }
          Field innerField = item.Fields[base.PresetFieldName];
          if (innerField == null)
          {
            Log.Error($"Preset field was not found in preset item '{item.ID}' ('{item.Paths.FullPath}')", base.GetType());
            keyValues = null;
            presetPercentages = null;
            break;
          }
          TrackingField field2 = new TrackingField(innerField);
          if (predicate == null)
          {
            predicate = profileData => profileData.ProfileID == profileItem.ID;
          }
          ContentProfile profile2 = field2.Profiles.FirstOrDefault<ContentProfile>(predicate);
          if (profile2 == null)
          {
            Log.Error($"Profile was not found in preset item '{item.ID}' ('{item.Paths.FullPath}')", base.GetType());
            keyValues = null;
            presetPercentages = null;
            break;
          }
          foreach (ContentProfileKeyData data3 in profile2.Keys)
          {
            if (!keyValues.ContainsKey(data3.Key))
            {
              keyValues.Add(data3.Key, new List<PresetKeyValue>());
            }
            PresetKeyValue value3 = new PresetKeyValue
            {
              PresetId = item.Key,
              KeyValue = data3.Value
            };
            keyValues[data3.Key].Add(value3);
          }
        }
      }
    }

    public void CustomizeProfile()
    {
      if (base.IsCustomData)
      {
        if (base.Presets.Items.Count > 0)
        {
          base.IsCustomData = false;
          this.SetCustomPresetCardVisibility(false);
          this.UpdateChart();
          this.ConfigureCustomizationButton();
        }
      }
      else
      {
        base.IsCustomData = true;
        this.SetCustomPresetCardVisibility(true);
        SheerResponse.SetInnerHtml(this.ChartContainer.ClientID, base.CustomChartHtml);
        foreach (string str in this.SliderJavaControlIds)
        {
          SheerResponse.Eval($"{str}.refreshValue();");
        }
        this.ConfigureCustomizationButton();
      }
    }

    private ContentProfile GetProfile(TrackingField field, Item profileItem)
    {
      Assert.ArgumentNotNull(field, "field");
      Assert.ArgumentNotNull(profileItem, "profileItem");
      return field.Profiles.FirstOrDefault<ContentProfile>(profileData => (profileData.ProfileID == profileItem.ID));
    }

    private void GetRecalculationProfileData(out Dictionary<string, List<PresetKeyValue>> keyValues, out Dictionary<string, float> presetPercentages)
    {
      keyValues = this.Page.Session["profileKeyValues"] as Dictionary<string, List<PresetKeyValue>>;
      this.Page.Session["profileKeyValues"] = null;
      Assert.IsNotNull(keyValues, "profile key values");
      presetPercentages = this.Page.Session["presetPercentages"] as Dictionary<string, float>;
      this.Page.Session["presetPercentages"] = null;
      Assert.IsNotNull(presetPercentages, "preset percents");
    }

    protected Item[] GetSelectedPresets()
    {
      string[] strArray = this.SelectedPresetIds.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
      List<Item> list = new List<Item>();
      foreach (string str in strArray)
      {
        Item item = base.GetDatabase().GetItem(new ID(str), base.Language);
        if (item != null)
        {
          list.Add(item);
        }
      }
      return list.ToArray();
    }

    private TrackingField GetTrackingField()
    {
      Item contextItem = this.GetContextItem();
      Assert.IsNotNull(contextItem, "dialog context item");
      TrackingField trackingField = ItemExtensions.GetTrackingField(contextItem);
      if (trackingField == null)
      {
        Log.Error($"Tracking field was not found in item '{contextItem.ID}' ('{contextItem.Paths.FullPath}')", base.GetType());
        return null;
      }
      return trackingField;
    }

    protected void InitializeButtons()
    {
      this.AddButton.Attributes["onclick"] = "return OnAddPreset('" + base.Presets.ClientID + "');";
      this.AddButton.Click = string.Empty;
      this.RemoveButton.Attributes["onclick"] = "return OnRemovePreset('" + this.SelectedPresetsContainer.ClientID + "');";
      this.RemoveButton.Click = string.Empty;
    }

    protected void InitializeCardControls(ContentProfile profile)
    {
      Assert.ArgumentNotNull(profile, "profile");
      base.CustomizationPresetCard.InitializeControl(profile);
      List<string> list = new List<string>();
      Assert.IsNotNull(base.CustomizationPresetCard.Sliders, "sliders");
      foreach (TextBoxSlider slider in base.CustomizationPresetCard.Sliders)
      {
        list.Add(slider.JavaControlId);
      }
      this.SliderJavaControlIds = list;
      HtmlTableCell parent = this.CustomizationCardContainer.Parent as HtmlTableCell;
      Assert.IsNotNull(parent, "custom card container");
      if (!base.IsCustomData)
      {
        parent.Style["display"] = "none";
      }
      else
      {
        parent.Style["display"] = "block";
      }
    }

    protected void InitializeChart(ContentProfile currentProfile)
    {
      Assert.ArgumentNotNull(currentProfile, "currentProfile");
      string javaControlId = this.PersonalizationRadarChart.JavaControlId;
      string iD = this.PersonalizationRadarChart.ID;
      Assert.IsNotNull(base.CustomizationPresetCard.Sliders, "sliders");
      this.PersonalizationRadarChart.SubscribeTo(base.CustomizationPresetCard.Sliders);
      this.PersonalizationRadarChart.RegisterScript = true;
      this.PersonalizationRadarChart.SkipDrawOnLoad = !base.IsCustomData;
      base.CustomChartHtml = HtmlUtil.RenderControl(this.PersonalizationRadarChart);
      base.CustomChartJavaControlId = this.PersonalizationRadarChart.JavaControlId;
      if (!base.IsCustomData)
      {
        this.PersonalizationRadarChart.JavaControlId = javaControlId;
        this.PersonalizationRadarChart.SkipDrawOnLoad = false;
        this.PersonalizationRadarChart.RegisterScript = false;
        this.PersonalizationRadarChart.ID = iD;
        this.PersonalizationRadarChart.SeriesName = Translate.Text("Custom");
        Dictionary<string, float> chartData = new Dictionary<string, float>();
        ContentProfileKeyData[] sortedProfileKeys = ProfileUtil.GetSortedProfileKeys(currentProfile);
        foreach (ContentProfileKeyData data in sortedProfileKeys)
        {
          if (!chartData.ContainsKey(HttpUtility.HtmlDecode(data.DisplayName)))
          {
            chartData.Add(HttpUtility.HtmlDecode(data.DisplayName), data.Value);
          }
        }
        this.PersonalizationRadarChart.BindTo(chartData);
      }
    }

    protected override void InitializeControls()
    {
      if (!XamlControl.AjaxScriptManager.IsEvent)
      {
        UrlHandle handle = UrlHandle.Get();
        string[] values = new string[2];
        values[0] = handle["allowpresetweight"];
        string str = StringUtil.GetString(values);
        if (!string.IsNullOrEmpty(str))
        {
          this.AllowPresetWeight = str == "1";
        }
        Item contextItem = this.GetContextItem();
        Assert.IsNotNull(contextItem, "dialog context item");
        Item profileItem = this.GetProfileItem();
        Assert.IsNotNull(profileItem, "profile");
        TrackingField trackingField = contextItem.GetTrackingField();
        if (trackingField == null)
        {
          string text = Translate.Text("Tracking field was not found in item '{0}' ('{1}')", new object[] { contextItem.ID, contextItem.Paths.FullPath });
          SheerResponse.Alert(text, new string[0]);
          Log.Error(string.Format(text, new object[0]), base.GetType());
        }
        else
        {
          ContentProfile profile = trackingField.Profiles.FirstOrDefault<ContentProfile>(profileData => profileData.ProfileID == profileItem.ID);

          if (profile == null)
          {
            string text = Translate.Text("Profile was not found.");
            SheerResponse.Alert(text, new string[0]);
            Log.Error(string.Format(text, new object[0]), base.GetType());
          }
          else
          {
            base.IsCustomData = (ProfileUtil.HasPresetData(profileItem, trackingField) && ((profile.Presets == null) || (profile.Presets.Count == 0))) || (ProfileUtil.GetPresets(profileItem).Length == 0);
            base.InitializeTooltip();
            this.InitializePresetList();
            this.InitializeButtons();
            this.InitializeCardControls(profile);
            this.InitializeChart(profile);
            this.InitializeSelectedPresets(profileItem, profile);
            this.ConfigureCustomizationButton();
            this.SetCustomPresetCardVisibility(base.IsCustomData);
            this.RenderSelecetdPresets();
            this.UpdateChart();
          }
        }
      }
    }

    protected System.Web.UI.Control InitializePresetCard(Item presetItem, float presetWeight, bool visible, out PresetCard card)
    {
      Assert.ArgumentNotNull(presetItem, "presetItem");
      PresetCard child = new PresetCard();
      card = child;
      child.InitializeControl(presetItem);
      child.TooltipManager = base.ToolTipManager;
      child.CssClass = "scPresetCard";
      child.ShowMoreInfoOnHover = true;
      if (this.AllowPresetWeight)
      {
        child.IsReadOnly = false;
        child.ShowPresetWeight = true;
        child.CardWeight = presetWeight;
      }
      HtmlGenericControl control = new HtmlGenericControl("div")
      {
        Attributes = {
                    ["class"] = "presetCardContainer",
                    ["scPresetId"] = presetItem.ID.ToShortID().ToString(),
                    ["scSliderId"] = child.Slider.JavaControlId,
                    ["ondblclick"] = "OnSelectedPresetDoubleClick(this);",
                    ["onmouseover"] = "hoverPresetCard(this);",
                    ["onmouseout"] = "clearHoveringPresetCard(this);",
                    ["onclick"] = "selectPresetCard(this);"
                }
      };
      if (!visible)
      {
        control.Style.Add("display", "none");
      }
      control.Controls.Add(child);
      return control;
    }

    protected virtual void InitializePresetCardManager(List<PresetCard> cards, Dictionary<string, List<double>> data)
    {
      Assert.ArgumentNotNull(cards, "cards");
      Assert.ArgumentNotNull(data, "data");
      if (this.AllowPresetWeight)
      {
        StringBuilder builder = new StringBuilder();
        StringBuilder builder2 = new StringBuilder();
        builder2.Append("[");
        for (int i = 0; i < cards.Count; i++)
        {
          builder2.Append(cards[i].Slider.JavaControlId);
          if (i < (cards.Count - 1))
          {
            builder2.Append(", ");
          }
        }
        builder2.Append("]");
        StringBuilder builder3 = new StringBuilder();
        builder3.Append("[");
        List<string> list = new List<string>(data.Keys);

        for (int j = 0; j < cards.Count; j++)
        {
          builder3.Append("[");
          for (int k = 0; k < list.Count; k++)
          {
            builder3.AppendFormat("{0}", data[list[k]][j]);
            if (k < (list.Count - 1))
            {
              builder3.Append(", ");
            }
          }
          builder3.Append("]");
          if (j < (cards.Count - 1))
          {
            builder3.Append(", ");
          }
        }
        builder3.Append("]");
        builder.AppendFormat("<script type='text/javascript'>var presetCardManager = null; document.observe('dom:loaded',function(){3} presetCardManager = new Sitecore.PresetCardManager({0}, {1}, {2}, {5});{4});</script>", new object[] { this.PersonalizationRadarChart.JavaControlId, builder2, builder3, "{", "}", this.SelectedPresetsContainer.ClientID });
        this.Page.ClientScript.RegisterClientScriptBlock(base.GetType(), "presetCardManager", builder.ToString());
      }
    }

    protected void InitializePresetList()
    {
      Item profileItem = this.GetProfileItem();
      if (profileItem != null)
      {
        List<BaseSelectPresetDialog.PresetInfo> list = new List<BaseSelectPresetDialog.PresetInfo>();
        foreach (Item item2 in ProfileUtil.GetPresets(profileItem))
        {
          BaseSelectPresetDialog.PresetInfo presetInfo = base.GetPresetInfo(item2);
          list.Add(presetInfo);
        }
        base.Presets.DataSource = list;
        base.Presets.DataValueField = "ShortID";
        base.Presets.OnClientItemDoubleClicked = "OnPresetItemDoubleClick";
        base.Presets.DataBind();
      }
    }

    protected void InitializeSelectedPresets(Item profileItem, ContentProfile currentProfile)
    {
      Func<ContentProfile, bool> predicate = null;
      Assert.ArgumentNotNull(profileItem, "profileItem");
      Assert.ArgumentNotNull(currentProfile, "currentProfile");
      Dictionary<string, float> presets = currentProfile.Presets;
      List<PresetCard> cards = new List<PresetCard>();
      Dictionary<string, List<double>> dictionary2 = new Dictionary<string, List<double>>();
      Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
      Dictionary<string, string> dictionary4 = new Dictionary<string, string>();

      ContentProfileKeyData[] sortedProfileKeys = ProfileUtil.GetSortedProfileKeys(currentProfile);

      foreach (ContentProfileKeyData data in sortedProfileKeys)
      {
        if (!dictionary2.ContainsKey(data.Name))
        {
          dictionary2.Add(data.Name, new List<double>());
        }
      }
      int num = 0;
      foreach (Item item in ProfileUtil.GetPresets(profileItem))
      {
        PresetCard card;
        bool visible = false;
        float presetWeight = 0f;
        if (presets != null)
        {
          foreach (string str in presets.Keys)
          {
            if (string.Compare(item.Key, str, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
              visible = true;
              presetWeight = presets[str];
              break;
            }
          }
        }
        if (visible && (num < presets.Keys.Count))
        {
          num++;
          this.AddPreset(item);
        }
        else
        {
          visible = false;
        }
        System.Web.UI.Control child = this.InitializePresetCard(item, presetWeight, visible, out card);
        cards.Add(card);
        this.SelectedPresetsContainer.Controls.Add(child);
        dictionary3.Add(item.ID.ToShortID().ToString().ToUpperInvariant(), card.Slider.ClientID);
        dictionary4.Add(item.ID.ToShortID().ToString().ToLowerInvariant(), card.Slider.JavaControlId);
        Field innerField = item.Fields[base.PresetFieldName];
        if (innerField == null)
        {
          foreach (string str2 in dictionary2.Keys)
          {
            dictionary2[str2].Add(0.0);
          }
        }
        else
        {
          TrackingField field2 = new TrackingField(innerField);
          if (predicate == null)
          {
            predicate = profileData => profileData.ProfileID == profileItem.ID;
          }
          ContentProfile profile = field2.Profiles.FirstOrDefault<ContentProfile>(predicate);
          if (profile == null)
          {
            foreach (string str3 in dictionary2.Keys)
            {
              dictionary2[str3].Add(0.0);
            }
          }
          else
          {
            foreach (ContentProfileKeyData data2 in profile.Keys)
            {
              dictionary2[data2.Name].Add((double)data2.Value);
            }
          }
        }
      }
      this.PresetToControlIdMap = dictionary3;
      this.PresetToSliderJavaControlIdMap = dictionary4;
      this.InitializePresetCardManager(cards, dictionary2);
    }

    private bool IsValuablePercent(float value) =>
        (Math.Abs(value) >= 0.01f);

    protected override void OK_Click()
    {
      Item profileItem = this.GetProfileItem();
      Assert.IsNotNull(profileItem, "profile");
      TrackingField trackingField = this.GetTrackingField();
      if (trackingField == null)
      {
        base.OK_Click();
      }
      else
      {
        ContentProfile profile = this.GetProfile(trackingField, profileItem);
        if (profile == null)
        {
          Log.Error(string.Format("Profile was not found.", new object[0]), base.GetType());
          base.OK_Click();
        }
        else
        {
          Dictionary<string, List<PresetKeyValue>> dictionary;
          Dictionary<string, float> dictionary2;
          this.CollectData(profileItem, profile, out dictionary, out dictionary2);
          Assert.IsNotNull(dictionary, "profile key values");
          Assert.IsNotNull(dictionary2, "percentages");
          if (this.PercentagesRequireRecalculation(dictionary2))
          {
            this.SaveRecalculationProfileData(dictionary, dictionary2);
            ContinuationManager.Current.Start(this, "Recalculate", new ClientPipelineArgs());
          }
          else
          {
            this.SaveProfileData(trackingField, profile, dictionary, dictionary2);
            base.OK_Click();
          }
        }
      }
    }

    public void OnAddPreset(string presetId)
    {
      Assert.ArgumentNotNull(presetId, "presetId");
      ID itemId = new ShortID(presetId).ToID();
      Item presetItem = base.GetDatabase().GetItem(itemId, base.Language);
      if (presetItem != null)
      {
        this.AddPreset(presetItem);
        this.RenderSelecetdPresets();
        this.UpdateChart();
      }
    }

    public void OnRemovePreset(string presetId)
    {
      Assert.ArgumentNotNull(presetId, "presetId");
      ID id = new ShortID(presetId).ToID();
      this.RemovePreset(id);
      this.RenderSelecetdPresets();
      this.UpdateChart();
    }

    private bool PercentagesRequireRecalculation(Dictionary<string, float> presetPercentages)
    {
      Assert.ArgumentNotNull(presetPercentages, "presetPercentages");
      if (!this.AllowPresetWeight)
      {
        return false;
      }
      float num = 0f;
      bool flag = false;
      foreach (string str in presetPercentages.Keys)
      {
        float num2 = presetPercentages[str];
        if (this.IsValuablePercent(num2))
        {
          flag = true;
        }
        num += num2;
      }
      return (flag && (Math.Abs((float)(num - 100f)) >= 1f));
    }

    protected virtual void Recalculate(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (args.IsPostBack)
      {
        if (string.Compare(args.Result, "recalculate", StringComparison.InvariantCultureIgnoreCase) == 0)
        {
          Item profileItem = this.GetProfileItem();
          Assert.IsNotNull(profileItem, "profile");
          TrackingField trackingField = this.GetTrackingField();
          if (trackingField == null)
          {
            base.OK_Click();
          }
          else if (this.GetProfile(trackingField, profileItem) == null)
          {
            Log.Error(string.Format("Profile was not found.", new object[0]), base.GetType());
            base.OK_Click();
          }
          else
          {
            Dictionary<string, List<PresetKeyValue>> dictionary;
            Dictionary<string, float> dictionary2;
            this.GetRecalculationProfileData(out dictionary, out dictionary2);
            Assert.IsNotNull(dictionary, "profile key values");
            Assert.IsNotNull(dictionary2, "preset percents");
            this.RecalculatePercents(dictionary2);
            if (this.AllowPresetWeight)
            {
              foreach (Item item2 in ProfileUtil.GetPresets(profileItem))
              {
                string key = item2.ID.ToShortID().ToString().ToLowerInvariant();
                if (this.PresetToSliderJavaControlIdMap.ContainsKey(key))
                {
                  string str2 = this.PresetToSliderJavaControlIdMap[key];
                  float num = 0f;
                  if (dictionary2.ContainsKey(item2.Key))
                  {
                    num = dictionary2[item2.Key];
                  }
                  SheerResponse.Eval($"{str2}.setValue('{num.FormatPercentage()}')");
                }
              }
            }
          }
        }
      }
      else
      {
        SheerResponse.ShowModalDialog(new UrlString(UIUtil.GetUri("control:Recalculate")).ToString(), "400px", "105px", string.Empty, true);
        args.WaitForPostBack();
      }
    }

    private void RecalculatePercents(Dictionary<string, float> presetPercentages)
    {
      Assert.ArgumentNotNull(presetPercentages, "presetPercentages");
      float num = 0f;
      List<string> list = new List<string>(presetPercentages.Keys);
      foreach (string str in list)
      {
        float num2 = presetPercentages[str];
        if (!this.IsValuablePercent(num2))
        {
          presetPercentages[str] = 0f;
        }
        else
        {
          num += num2;
        }
      }
      if (this.IsValuablePercent(num))
      {
        double num3 = 100f / num;
        foreach (string str2 in list)
        {
          float num4 = presetPercentages[str2];
          if (this.IsValuablePercent(num4))
          {
            presetPercentages[str2] = (float)Math.Round((double)(num4 * num3), 0);
          }
        }
        num = 0f;
        foreach (string str3 in list)
        {
          float num5 = presetPercentages[str3];
          if (this.IsValuablePercent(num5))
          {
            num += num5;
          }
        }
        float num6 = 100f - num;
        string str4 = string.Empty;
        if (num6 < 0f)
        {
          float minValue = float.MinValue;
          foreach (string str5 in list)
          {
            float num8 = presetPercentages[str5];
            if (this.IsValuablePercent(num8) && (minValue < num8))
            {
              str4 = str5;
              minValue = num8;
            }
          }
        }
        else
        {
          float maxValue = float.MaxValue;
          foreach (string str6 in list)
          {
            float num10 = presetPercentages[str6];
            if (this.IsValuablePercent(num10) && (maxValue > num10))
            {
              str4 = str6;
              maxValue = num10;
            }
          }
        }
        Assert.IsNotNullOrEmpty(str4, "key");
        presetPercentages[str4] += num6;
      }
    }

    protected void RemovePreset(ID presetId)
    {
      Assert.ArgumentNotNull(presetId, "presetId");
      string[] strArray = this.SelectedPresetIds.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
      StringBuilder builder = new StringBuilder();
      foreach (string str in strArray)
      {
        if (string.Compare(str, presetId.ToString(), StringComparison.InvariantCultureIgnoreCase) != 0)
        {
          builder.Append("|" + str + "|");
        }
      }
      this.SelectedPresetIds = builder.ToString();
    }

    protected void RemovePreset(Item presetItem)
    {
      Assert.ArgumentNotNull(presetItem, "presetItem");
      this.RemovePreset(presetItem.ID);
    }

    protected void RenderSelecetdPresets()
    {
      Item[] selectedPresets = this.GetSelectedPresets();
      Item profileItem = this.GetProfileItem();
      if (profileItem != null)
      {
        foreach (Item item2 in ProfileUtil.GetPresets(profileItem))
        {
          bool flag = false;
          foreach (Item item3 in selectedPresets)
          {
            if (item2.ID == item3.ID)
            {
              flag = true;
              break;
            }
          }
          SheerResponse.Eval(string.Concat(new object[] { "SetPresetCardVisibility('", item2.ID.ToShortID(), "', ", flag.ToString().ToLowerInvariant(), ", '", this.SelectedPresetsContainer.ClientID, "')" }));
        }
      }
    }

    private void SaveProfileData(TrackingField field, ContentProfile profile, Dictionary<string, List<PresetKeyValue>> keyValues, Dictionary<string, float> presetPercentages)
    {
      Assert.ArgumentNotNull(field, "field");
      Assert.ArgumentNotNull(profile, "profile");
      Assert.ArgumentNotNull(keyValues, "keyValues");
      Assert.ArgumentNotNull(presetPercentages, "presetPercentages");
      Dictionary<string, float> dictionary = this.CalculateData(profile, keyValues, presetPercentages);
      Assert.IsNotNull(dictionary, "profile data");
      this.SaveProfileData(field, profile, dictionary, presetPercentages);
      SheerResponse.SetDialogValue("refresh");
    }

    private void SaveProfileData(TrackingField field, ContentProfile profile, Dictionary<string, float> chartData, Dictionary<string, float> presetPercentages)
    {
      Assert.ArgumentNotNull(field, "field");
      Assert.ArgumentNotNull(profile, "profile");
      Assert.ArgumentNotNull(chartData, "chartData");
      Assert.ArgumentNotNull(presetPercentages, "presetPercentages");
      int num = 0;
      foreach (string str in chartData.Keys)
      {
        for (int i = 0; i < profile.Keys.Length; i++)
        {
          if (string.Compare(profile.Keys[i].Key, str, StringComparison.InvariantCultureIgnoreCase) == 0)
          {
            profile.Keys[i].Value = chartData[str];
            if (profile.Keys[i].Value == profile.Keys[i].DefaultValue)
            {
              num++;
            }
          }
        }
      }
      profile.Presets = presetPercentages;
      profile.SaveToField = ((num != profile.Keys.Count<ContentProfileKeyData>()) || base.IsCustomData) || (this.GetSelectedPresets().Length > 0);
      using (new StatisticDisabler(StatisticDisablerState.ForItemsWithoutVersionOnly))
      {
        field.InnerField.Item.Editing.BeginEdit();
        field.AcceptChanges();
        field.InnerField.Item.Editing.EndEdit();
      }
    }

    private void SaveRecalculationProfileData(Dictionary<string, List<PresetKeyValue>> keyValues, Dictionary<string, float> presetPercentages)
    {
      Assert.ArgumentNotNull(keyValues, "keyValues");
      Assert.ArgumentNotNull(presetPercentages, "presetPercentages");
      this.Page.Session["profileKeyValues"] = keyValues;
      this.Page.Session["presetPercentages"] = presetPercentages;
    }

    protected void SetCustomPresetCardVisibility(bool visible)
    {
      this.AddButton.Disabled = visible;
      this.RemoveButton.Disabled = visible;
      string javascript = (visible ? "DisableListBox(" : "EnableListBox(") + "'" + base.Presets.ClientID + "');";
      if (XamlControl.AjaxScriptManager.IsEvent)
      {
        SheerResponse.SetStyle(this.CustomizationCardContainer.Parent.ClientID, "display", visible ? "block" : "none");
        SheerResponse.SetStyle(this.SelectedPresetsContainer.ClientID, "visibility", visible ? "hidden" : "visible");
        SheerResponse.Eval(javascript);
      }
      else
      {
        base.Presets.Enabled = !visible;
      }
    }

    protected void UpdateChart()
    {
      if (this.AllowPresetWeight)
      {
        SheerResponse.Eval("presetCardManager.RedrawChart()");
      }
      else
      {
        this.PersonalizationRadarChart.SeriesName = Translate.Text("Custom");
        Item[] selectedPresets = this.GetSelectedPresets();
        Dictionary<string, float> chartData = new Dictionary<string, float>();
        if (selectedPresets.Length == 0)
        {
          Item contextItem = this.GetContextItem();
          Assert.IsNotNull(contextItem, "dialog context item");
          Item profileItem = this.GetProfileItem();
          Assert.IsNotNull(profileItem, "profile");
          TrackingField trackingField = contextItem.GetTrackingField();
          if (trackingField == null)
          {
            string text = Translate.Text("Tracking field was not found in item '{0}' ('{1}')", new object[] { contextItem.ID, contextItem.Paths.FullPath });
            SheerResponse.Alert(text, new string[0]);
            Log.Error(string.Format(text, new object[0]), base.GetType());
            return;
          }
          ContentProfile profile = trackingField.Profiles.FirstOrDefault<ContentProfile>(profileData => profileData.ProfileID == profileItem.ID);
          Assert.IsNotNull(profile, "content profile");
          foreach (ContentProfileKeyData data in this.GetSortedProfileKeys(profile))
          {
            if (!chartData.ContainsKey(HttpUtility.HtmlDecode(data.DisplayName)))
            {
              chartData.Add(HttpUtility.HtmlDecode(data.DisplayName), data.DefaultValue);
            }
          }
        }
        else
        {
          Func<ContentProfile, bool> predicate = null;
          Item item1 = this.GetProfileItem();
          if (item1 == null)
          {
            Log.Error(string.Format("Profile item not found.", new object[0]), base.GetType());
            return;
          }
          foreach (Item item2 in selectedPresets)
          {
            Field innerField = item2.Fields[base.PresetFieldName];
            if (innerField == null)
            {
              Log.Error($"Profile Card Value field was not found in preset item '{item2.ID}' ('{item2.Paths.FullPath}')", base.GetType());
              return;
            }
            TrackingField field3 = new TrackingField(innerField);
            if (predicate == null)
            {
              predicate = profileData => profileData.ProfileID == item1.ID;
            }
            ContentProfile profile = field3.Profiles.FirstOrDefault<ContentProfile>(predicate);
            if (profile == null)
            {
              return;
            }
            foreach (ContentProfileKeyData data2 in ProfileUtil.GetSortedProfileKeys(profile))
            {
              Dictionary<string, float> dictionary2;
              string str4;
              if (!chartData.ContainsKey(HttpUtility.HtmlDecode(data2.DisplayName)))
              {
                chartData.Add(HttpUtility.HtmlDecode(data2.DisplayName), 0f);
              }
                (dictionary2 = chartData)[str4 = HttpUtility.HtmlDecode(data2.DisplayName)] = dictionary2[str4] + data2.Value;
            }
          }
          List<string> list = new List<string>(chartData.Keys);
          foreach (string str2 in list)
          {
            chartData[str2] /= (float)selectedPresets.Length;
          }
        }
        this.PersonalizationRadarChart.BindTo(chartData);
        this.PersonalizationRadarChart.RegisterScript = false;
        string str3 = HtmlUtil.RenderControl(this.PersonalizationRadarChart);
        SheerResponse.SetInnerHtml(this.ChartContainer.ClientID, str3);
      }
    }

    private IEnumerable<ContentProfileKeyData> GetSortedProfileKeys(ContentProfile profile)
    {
      List<ContentProfileKeyData> list = new List<ContentProfileKeyData>();
      Item profileItem = profile.GetProfileItem();
      List<Item> list2 = new List<Item>();
      foreach (ContentProfileKeyData data in profile.Keys)
      {
        Item innerItem = data.InnerItem;
        if (innerItem != null)
        {
          list2.Add(innerItem);
        }
      }
      return list.ToArray();
    }

    public Dictionary<string, string> PresetToControlIdMap
    {
      get
      {
        Dictionary<string, string> dictionary = this.ViewState["PresetToControlIdMap"] as Dictionary<string, string>;
        if (dictionary == null)
        {
          return new Dictionary<string, string>();
        }
        return dictionary;
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["PresetToControlIdMap"] = value;
      }
    }

    public Dictionary<string, string> PresetToSliderJavaControlIdMap
    {
      get
      {
        Dictionary<string, string> dictionary = this.ViewState["PresetToSliderJavaControlIdMap"] as Dictionary<string, string>;
        if (dictionary == null)
        {
          return new Dictionary<string, string>();
        }
        return dictionary;
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["PresetToSliderJavaControlIdMap"] = value;
      }
    }

    protected Button AddButton { get; set; }

    protected bool AllowPresetWeight
    {
      get
      {
        return (((this.ViewState["AllowPresetWeight"] as string) ?? string.Empty) == "1");
      }
      set
      {
        this.ViewState["AllowPresetWeight"] = value ? "1" : string.Empty;
      }
    }

    protected HtmlGenericControl ChartContainer { get; set; }

    protected HtmlGenericControl CustomizationCardContainer { get; set; }

    protected RadarChart PersonalizationRadarChart { get; set; }

    protected Button RemoveButton { get; set; }

    protected string SelectedPresetIds
    {
      get
      {
        return ((this.ViewState["SelectedPresetIds"] as string) ?? string.Empty);
      }
      set
      {
        Assert.IsNotNull(value, "selcted presets cannot be null");
        this.ViewState["SelectedPresetIds"] = value;
      }
    }

    protected List<string> SliderJavaControlIds
    {
      get
      {
        List<string> list = new List<string>();
        string str = (this.ViewState["SliderJavaControlIds"] as string) ?? string.Empty;
        foreach (string str2 in str.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
          if (!string.IsNullOrEmpty(str2))
          {
            list.Add(str2);
          }
        }
        return list;
      }
      set
      {
        Assert.IsNotNull(value, "value");
        StringBuilder builder = new StringBuilder();
        foreach (string str in value)
        {
          builder.Append(str + "|");
        }
        this.ViewState["SliderJavaControlIds"] = builder.ToString();
      }
    }

    protected HtmlGenericControl SelectedPresetsContainer { get; set; }

    private class PresetKeyValue
    {
      public float KeyValue { get; set; }

      public string PresetId { get; set; }
    }
  }
}
