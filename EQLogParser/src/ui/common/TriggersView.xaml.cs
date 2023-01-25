﻿using FontAwesome5;
using Syncfusion.UI.Xaml.TreeView;
using Syncfusion.Windows.PropertyGrid;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EQLogParser
{
  /// <summary>
  /// Interaction logic for TriggersView.xaml
  /// </summary>
  public partial class TriggersView : UserControl, IDisposable
  {
    private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private const string LABEL_NEW_OVERLAY = "New Overlay";
    private const string LABEL_NEW_TRIGGER = "New Trigger";
    private const string LABEL_NEW_FOLDER = "New Folder";
    private WrapTextEditor ErrorEditor;
    private List<TriggerNode> Removed;
    private SpeechSynthesizer TestSynth = null;

    public TriggersView()
    {
      InitializeComponent();

      try
      {
        TestSynth = new SpeechSynthesizer();
        TestSynth.SetOutputToDefaultAudioDevice();
        voices.ItemsSource = TestSynth.GetInstalledVoices().Select(voice => voice.VoiceInfo.Name).ToList();
      }
      catch (Exception)
      {
        // may not initialize on all systems
      }

      if (ConfigUtil.IfSetOrElse("TriggersWatchForGINA", false))
      {
        watchGina.IsChecked = true;
      }

      var selectedVoice = TriggerUtil.GetSelectedVoice();
      if (voices.ItemsSource is List<string> populated && populated.IndexOf(selectedVoice) is int found && found > -1)
      {
        voices.SelectedIndex = found;
      }

      rateOption.SelectedIndex = TriggerUtil.GetVoiceRate();

      if (MainWindow.CurrentLogFile == null)
      {
        SetPlayer("Activate Triggers", "EQDisabledBrush", EFontAwesomeIcon.Solid_Play, false);
      }
      else
      {
        EventsLogLoadingComplete(this, true);
      }

      treeView.DragDropController = new TreeViewDragDropController();
      treeView.DragDropController.CanAutoExpand = true;
      treeView.DragDropController.AutoExpandDelay = new TimeSpan(0, 0, 1);

      var priorityEditor = new CustomEditor();
      priorityEditor.Editor = new RangeEditor(1, 5);
      priorityEditor.Properties.Add("Priority");
      thePropertyGrid.CustomEditorCollection.Add(priorityEditor);

      var textWrapEditor = new CustomEditor();
      textWrapEditor.Editor = new WrapTextEditor();
      textWrapEditor.Properties.Add("Comments");
      textWrapEditor.Properties.Add("OverlayComments");
      textWrapEditor.Properties.Add("Pattern");
      textWrapEditor.Properties.Add("EndPattern");
      textWrapEditor.Properties.Add("CancelPattern");
      textWrapEditor.Properties.Add("EndTextToSpeak");
      textWrapEditor.Properties.Add("TextToSpeak");
      textWrapEditor.Properties.Add("WarningToSpeak");
      thePropertyGrid.CustomEditorCollection.Add(textWrapEditor);

      var errorEditor = new CustomEditor();
      errorEditor.Editor = ErrorEditor = new WrapTextEditor();
      errorEditor.Properties.Add("Errors");
      thePropertyGrid.CustomEditorCollection.Add(errorEditor);

      var colorEditor = new CustomEditor();
      colorEditor.Editor = new ColorEditor();
      colorEditor.Properties.Add("FontBrush");
      colorEditor.Properties.Add("PrimaryBrush");
      colorEditor.Properties.Add("SecondaryBrush");
      thePropertyGrid.CustomEditorCollection.Add(colorEditor);

      var listEditor = new CustomEditor();
      listEditor.Editor = new TriggerListsEditor();
      listEditor.Properties.Add("TriggerAgainOption");
      listEditor.Properties.Add("FontSize");
      listEditor.Properties.Add("SortBy");
      thePropertyGrid.CustomEditorCollection.Add(listEditor);

      var timeEditor = new CustomEditor();
      timeEditor.Editor = new RangeEditor(0, 60);
      timeEditor.Properties.Add("Seconds");
      timeEditor.Properties.Add("Minutes");
      thePropertyGrid.CustomEditorCollection.Add(timeEditor);

      var exampleEditor = new CustomEditor();
      exampleEditor.Editor = new ExampleTimerBar();
      exampleEditor.Properties.Add("TimerBarPreview");
      thePropertyGrid.CustomEditorCollection.Add(exampleEditor);

      treeView.Nodes.Add(TriggerManager.Instance.GetTriggerTreeView());
      treeView.Nodes.Add(TriggerManager.Instance.GetOverlayTreeView());

      TriggerManager.Instance.EventsUpdateTree += EventsUpdateTriggerTree;
      TriggerManager.Instance.EventsSelectTrigger += EventsSelectTrigger;
      (Application.Current.MainWindow as MainWindow).EventsLogLoadingComplete += EventsLogLoadingComplete;
    }

    private void CollapseAllClick(object sender, RoutedEventArgs e)
    {
      treeView.CollapseAll();
      SaveNodeExpanded(treeView.Nodes.Cast<TriggerTreeViewNode>().ToList());
    }

    private void ExpandAllClick(object sender, RoutedEventArgs e)
    {
      treeView.ExpandAll();
      SaveNodeExpanded(treeView.Nodes.Cast<TriggerTreeViewNode>().ToList());
    }

    private void ExportClick(object sender, RoutedEventArgs e) => TriggerUtil.Export(treeView.Nodes, treeView.SelectedItems?.Cast<TriggerTreeViewNode>().ToList());
    private void ImportClick(object sender, RoutedEventArgs e) => TriggerUtil.Import(treeView?.SelectedItem as TriggerTreeViewNode);
    private void RenameClick(object sender, RoutedEventArgs e) => treeView.BeginEdit(treeView.SelectedItem as TriggerTreeViewNode);
    private void EventsSelectTrigger(object sender, Trigger e) => SelectFile(e);

    private void EventsLogLoadingComplete(object sender, bool e)
    {
      if (TriggerManager.Instance.IsActive())
      {
        SetPlayer("Deactivate Triggers", "EQStopForegroundBrush", EFontAwesomeIcon.Solid_Square);
      }
      else
      {
        SetPlayer("Activate Triggers", "EQMenuIconBrush", EFontAwesomeIcon.Solid_Play);
      }
    }

    private void EventsUpdateTriggerTree(object sender, bool e) => Dispatcher.InvokeAsync(() => RefreshTriggerNode());

    private void RefreshTriggerNode()
    {
      treeView.Nodes.Remove(treeView.Nodes[0]);
      treeView.Nodes.Insert(0, TriggerManager.Instance.GetTriggerTreeView());
    }

    private void RefreshOverlayNode()
    {
      treeView.Nodes.Remove(treeView.Nodes[1]);
      treeView.Nodes.Add(TriggerManager.Instance.GetOverlayTreeView());
    }

    private void OptionsChanged(object sender, RoutedEventArgs e)
    {
      // one way to see if UI has been initialized
      if (startIcon?.Icon != FontAwesome5.EFontAwesomeIcon.None)
      {
        if (sender == watchGina)
        {
          ConfigUtil.SetSetting("TriggersWatchForGINA", watchGina.IsChecked.Value.ToString(CultureInfo.CurrentCulture));
        }
        else if (sender == voices)
        {
          if (voices.SelectedValue is string voiceName)
          {
            ConfigUtil.SetSetting("TriggersSelectedVoice", voiceName);
            TriggerManager.Instance.SetVoice(voiceName);

            if (TestSynth != null)
            {
              TestSynth.SelectVoice(voiceName);
              TestSynth.SpeakAsync(voiceName);
            }
          }
        }
        else if (sender == rateOption)
        {
          ConfigUtil.SetSetting("TriggersVoiceRate", rateOption.SelectedIndex.ToString(CultureInfo.CurrentCulture));
          TriggerManager.Instance.SetVoiceRate(rateOption.SelectedIndex);

          if (TestSynth != null)
          {
            TestSynth.Rate = rateOption.SelectedIndex;
            var rateText = rateOption.SelectedIndex == 0 ? "Default Voice Rate" : "Voice Rate " + rateOption.SelectedIndex.ToString();
            TestSynth.SpeakAsync(rateText);
          }
        }
      }
    }

    private void SelectFile(object file)
    {
      if (file != null)
      {
        bool selectFile = false;
        bool isTrigger = false;
        if (treeView.SelectedItem == null)
        {
          selectFile = true;
        }
        else if (treeView.SelectedItem is TriggerTreeViewNode node && node.SerializedData != null)
        {
          if (node.SerializedData.TriggerData == null || node.SerializedData.TriggerData != file)
          {
            isTrigger = true;
            selectFile = true;
          }
          else if (node.SerializedData.OverlayData == null || node.SerializedData.OverlayData != file)
          {
            selectFile = true;
          }
        }

        if (selectFile)
        {
          var found = FindAndExpandNode((isTrigger ? treeView.Nodes[0] : treeView.Nodes[1]) as TriggerTreeViewNode, file);
          treeView.SelectedItem = found;
          SelectionChanged(found);
        }
      }
    }

    private void SetPlayer(string title, string brush, EFontAwesomeIcon icon, bool hitTest = true)
    {
      startIcon.Icon = icon;
      startIcon.SetResourceReference(ImageAwesome.ForegroundProperty, brush);
      titleLabel.SetResourceReference(Label.ForegroundProperty, brush);
      titleLabel.Content = title;
      startButton.IsHitTestVisible = hitTest;
    }

    private void PlayButtonClick(object sender, RoutedEventArgs e)
    {
      if (startIcon.Icon == EFontAwesomeIcon.Solid_Play)
      {
        SetPlayer("Deactivate Triggers", "EQStopForegroundBrush", EFontAwesomeIcon.Solid_Square);
        TriggerManager.Instance.Start();
      }
      else
      {
        SetPlayer("Activate Triggers", "EQMenuIconBrush", EFontAwesomeIcon.Solid_Play);
        TriggerManager.Instance.Stop();
      }
    }

    private void ItemDropping(object sender, TreeViewItemDroppingEventArgs e)
    {
      var target = e.TargetNode as TriggerTreeViewNode;

      if (e.DropPosition == DropPosition.None)
      {
        e.Handled = true;
        return;
      }

      if (target.Level == 0 && e.DropPosition != DropPosition.DropAsChild)
      {
        e.Handled = true;
        return;
      }

      // fix drag and drop that wants to reverse the order for some reason
      var list = e.DraggingNodes.Cast<TriggerTreeViewNode>().ToList();
      list.Reverse();

      if ((target == treeView.Nodes[1] || target.ParentNode == treeView.Nodes[1]) && list.Any(item => !item.IsOverlay))
      {
        e.Handled = true;
        return;
      }

      if ((target != treeView.Nodes[1] && target.ParentNode != treeView.Nodes[1]) && list.Any(item => item.IsOverlay))
      {
        e.Handled = true;
        return;
      }

      e.DraggingNodes.Clear();
      list.ForEach(node => e.DraggingNodes.Add(node));
      target = ((!target.IsTrigger && !target.IsOverlay) && e.DropPosition == DropPosition.DropAsChild) ? target : target.ParentNode as TriggerTreeViewNode;

      Removed = new List<TriggerNode>();
      foreach (var node in e.DraggingNodes.Cast<TriggerTreeViewNode>())
      {
        if (node.ParentNode != target)
        {
          if (node.ParentNode is TriggerTreeViewNode parent && parent.SerializedData != null && parent.SerializedData.Nodes != null)
          {
            parent.SerializedData.Nodes.Remove(node.SerializedData);
            Removed.Add(node.SerializedData);
          }
        }
      }
    }

    private void ItemDropped(object sender, TreeViewItemDroppedEventArgs e)
    {
      var target = e.TargetNode as TriggerTreeViewNode;
      target = ((!target.IsTrigger && !target.IsOverlay) && e.DropPosition == DropPosition.DropAsChild) ? target : target.ParentNode as TriggerTreeViewNode;

      if (target.SerializedData != null)
      {
        if (target.SerializedData.Nodes == null || target.SerializedData.Nodes.Count == 0)
        {
          target.SerializedData.Nodes = e.DraggingNodes.Cast<TriggerTreeViewNode>().Select(node => node.SerializedData).ToList();
          target.SerializedData.IsExpanded = true;
        }
        else
        {
          var newList = new List<TriggerNode>();
          var sources = target.SerializedData.Nodes.ToList();

          if (Removed != null)
          {
            sources.AddRange(Removed);
          }

          foreach (var viewNode in target.ChildNodes.Cast<TriggerTreeViewNode>())
          {
            var found = sources.Find(source => source == viewNode.SerializedData);
            if (found != null)
            {
              newList.Add(found);
              sources.Remove(found);
            }
          }

          if (sources.Count > 0)
          {
            newList.AddRange(sources);
          }

          target.SerializedData.Nodes = newList;
        }
      }

      TriggerManager.Instance.UpdateTriggers(false);
      TriggerManager.Instance.UpdateOverlays();
      RefreshTriggerNode();
      SelectionChanged(null);
    }

    private void CreateNodeClick(object sender, RoutedEventArgs e)
    {
      if (treeView.SelectedItem != null && treeView.SelectedItem is TriggerTreeViewNode node)
      {
        var newNode = new TriggerNode { Name = LABEL_NEW_FOLDER };
        node.SerializedData.Nodes = (node.SerializedData.Nodes == null) ? new List<TriggerNode>() : node.SerializedData.Nodes;
        node.SerializedData.IsExpanded = true;
        node.SerializedData.Nodes.Add(newNode);
        TriggerManager.Instance.UpdateTriggers();
        RefreshTriggerNode();
      }
    }

    private void CreateTimerOverlayClick(object sender, RoutedEventArgs e)
    {
      if (treeView.SelectedItem != null && treeView.SelectedItem is TriggerTreeViewNode node)
      {
        var newNode = new TriggerNode
        {
          Name = LABEL_NEW_OVERLAY,
          IsEnabled = true,
          OverlayData = new Overlay { Name = LABEL_NEW_OVERLAY, Id = Guid.NewGuid().ToString(), IsTimerOverlay = true }
        };

        node.SerializedData.Nodes = (node.SerializedData.Nodes == null) ? new List<TriggerNode>() : node.SerializedData.Nodes;
        node.SerializedData.IsExpanded = true;
        node.SerializedData.Nodes.Add(newNode);
        TriggerManager.Instance.UpdateOverlays();
        RefreshOverlayNode();
        SelectFile(newNode.OverlayData);
      }
    }

    private void CreateTriggerClick(object sender, RoutedEventArgs e)
    {
      if (treeView.SelectedItem != null && treeView.SelectedItem is TriggerTreeViewNode node)
      {
        var newNode = new TriggerNode { Name = LABEL_NEW_TRIGGER, IsEnabled = true, TriggerData = new Trigger { Name = LABEL_NEW_TRIGGER } };
        node.SerializedData.Nodes = (node.SerializedData.Nodes == null) ? new List<TriggerNode>() : node.SerializedData.Nodes;
        node.SerializedData.IsExpanded = true;
        node.SerializedData.Nodes.Add(newNode);
        TriggerManager.Instance.UpdateTriggers();
        RefreshTriggerNode();
        SelectFile(newNode.TriggerData);
      }
    }

    private void DeleteClick(object sender, RoutedEventArgs e)
    {
      if (treeView.SelectedItems != null)
      {
        bool updateTriggers = false;
        bool updateOverlays = false;
        foreach (var node in treeView.SelectedItems.Cast<TriggerTreeViewNode>())
        {
          if (node.ParentNode is TriggerTreeViewNode parent)
          {
            parent.SerializedData.Nodes.Remove(node.SerializedData);
            if (parent.SerializedData.Nodes.Count == 0)
            {
              parent.SerializedData.IsEnabled = false;
              parent.SerializedData.IsExpanded = false;
            }

            if (parent == treeView.Nodes[1])
            {
              updateOverlays = true;
            }
            else
            {
              updateTriggers = true;
            }
          }
        }

        thePropertyGrid.SelectedObject = null;
        thePropertyGrid.IsEnabled = false;
        thePropertyGrid.DescriptionPanelVisibility = Visibility.Collapsed;
        buttonPanel.Visibility = Visibility.Collapsed;

        if (updateTriggers)
        {
          TriggerManager.Instance.UpdateTriggers();
          RefreshTriggerNode();
        }

        if (updateOverlays)
        {
          TriggerManager.Instance.UpdateOverlays();
          RefreshOverlayNode();
        }
      }
    }

    private void DisableNodes(TriggerNode node)
    {
      if (node.TriggerData == null && node.OverlayData == null)
      {
        node.IsEnabled = false;
        node.IsExpanded = false;
        if (node.Nodes != null)
        {
          foreach (var child in node.Nodes)
          {
            DisableNodes(child);
          }
        }
      }
    }

    private void NodeExpanded(object sender, NodeExpandedCollapsedEventArgs e)
    {
      if (e.Node is TriggerTreeViewNode node)
      {
        node.SerializedData.IsExpanded = node.IsExpanded;
      }
    }

    private void ItemEndEdit(object sender, TreeViewItemEndEditEventArgs e)
    {
      if (!e.Cancel && e.Node is TriggerTreeViewNode node)
      {
        var previous = node.Content as string;
        // delay because node still shows old value
        Dispatcher.InvokeAsync(() =>
        {
          var content = node.Content as string;
          if (string.IsNullOrEmpty(content) || content.Trim().Length == 0)
          {
            node.Content = previous;
          }
          else
          {
            node.SerializedData.Name = node.Content as string;
            if (node.IsTrigger && node.SerializedData.TriggerData != null)
            {
              node.SerializedData.TriggerData.Name = node.Content as string;
            }
            else if (node.IsOverlay && node.SerializedData.OverlayData != null)
            {
              node.SerializedData.OverlayData.Name = node.Content as string;
            }

            TriggerManager.Instance.UpdateTriggers(false);
            TriggerManager.Instance.UpdateOverlays();
          }
        }, System.Windows.Threading.DispatcherPriority.Background);
      }
    }

    private void ItemContextMenuOpening(object sender, ItemContextMenuOpeningEventArgs e)
    {
      var node = treeView.SelectedItem as TriggerTreeViewNode;
      var count = (treeView.SelectedItems != null) ? treeView.SelectedItems.Count : 0;


      if (node != null)
      {
        deleteTriggerMenuItem.IsEnabled = (node != treeView.Nodes[0] && node != treeView.Nodes[1]) || count > 1;
        renameMenuItem.IsEnabled = node != treeView.Nodes[0] && node != treeView.Nodes[1] && count == 1;
        importMenuItem.IsEnabled = !node.IsTrigger && !node.IsOverlay && node != treeView.Nodes[1] && count == 1;
        exportMenuItem.IsEnabled = treeView.SelectedItems.Cast<TriggerTreeViewNode>().Any(node => !node.IsOverlay && node != treeView.Nodes[1]);
        newMenuItem.IsEnabled = !node.IsTrigger && !node.IsOverlay && count == 1;
      }
      else
      {
        deleteTriggerMenuItem.IsEnabled = false;
        renameMenuItem.IsEnabled = false;
        importMenuItem.IsEnabled = false;
        exportMenuItem.IsEnabled = false;
        newMenuItem.IsEnabled = false;
      }

      importMenuItem.Header = importMenuItem.IsEnabled ? "Import to " + node.Content.ToString() : "Import";
      if (newMenuItem.IsEnabled)
      {
        newFolder.Visibility = node == treeView.Nodes[1] ? Visibility.Collapsed : Visibility.Visible;
        newTrigger.Visibility = node == treeView.Nodes[1] ? Visibility.Collapsed : Visibility.Visible;
        newTimerOverlay.Visibility = node == treeView.Nodes[1] ? Visibility.Visible : Visibility.Collapsed;
      }
    }

    private void SelectionChanged(object sender, ItemSelectionChangedEventArgs e)
    {
      if (e.AddedItems.Count > 0 && e.AddedItems[0] is TriggerTreeViewNode node)
      {
        SelectionChanged(node);
      }
    }

    private void SelectionChanged(TriggerTreeViewNode node)
    {
      dynamic model = null;
      var isTrigger = (node?.IsTrigger == true);
      var isOverlay = (node?.IsOverlay == true);

      if (isTrigger || isOverlay)
      {
        if (isTrigger)
        {
          model = new TriggerPropertyModel { Original = node.SerializedData.TriggerData };
          TriggerUtil.Copy(model, node.SerializedData.TriggerData);
        }
        else if (isOverlay)
        {
          model = new OverlayPropertyModel { Original = node.SerializedData.OverlayData };
          TriggerUtil.Copy(model, node.SerializedData.OverlayData);
          model.TimerBarPreview = model.Id;
        }

        saveButton.IsEnabled = false;
        cancelButton.IsEnabled = false;
      }

      thePropertyGrid.SelectedObject = model;
      thePropertyGrid.IsEnabled = (thePropertyGrid.SelectedObject != null);
      thePropertyGrid.DescriptionPanelVisibility = (isTrigger || isOverlay) ? Visibility.Visible : Visibility.Collapsed;
      buttonPanel.Visibility = (isTrigger || isOverlay) ? Visibility.Visible : Visibility.Collapsed;

      if (isTrigger)
      {
        PropertyGridUtil.EnableCategories(thePropertyGrid, new[]
        {
          new { Name = timerDurationItem.CategoryName, IsEnabled = node.SerializedData.TriggerData.EnableTimer },
          new { Name = patternItem.CategoryName, IsEnabled = true },
          new { Name = evalTimeItem.CategoryName, IsEnabled = true },
          new { Name = fontSizeItem.CategoryName, IsEnabled = false },
        });
      }
      else if (isOverlay)
      {
        PropertyGridUtil.EnableCategories(thePropertyGrid, new[]
        {
          new { Name = timerDurationItem.CategoryName, IsEnabled = false },
          new { Name = patternItem.CategoryName, IsEnabled = false },
          new { Name = evalTimeItem.CategoryName, IsEnabled = false },
          new { Name = fontSizeItem.CategoryName, IsEnabled = true },
        });
      }
    }

    private void NodeChecked(object sender, NodeCheckedEventArgs e)
    {
      if (e.Node is TriggerTreeViewNode node)
      {
        node.SerializedData.IsEnabled = node.IsChecked;

        if (!node.IsTrigger && !node.IsOverlay)
        {
          CheckParent(node);
          CheckChildren(node, node.IsChecked);
        }

        TriggerManager.Instance.UpdateTriggers();
        TriggerManager.Instance.UpdateOverlays();
      }
    }

    private void CheckChildren(TriggerTreeViewNode node, bool? value)
    {
      foreach (var child in node.ChildNodes.Cast<TriggerTreeViewNode>())
      {
        child.SerializedData.IsEnabled = value;
        if (!child.IsTrigger && !child.IsOverlay)
        {
          CheckChildren(child, value);
        }
      }
    }

    private void CheckParent(TriggerTreeViewNode node)
    {
      if (node.ParentNode is TriggerTreeViewNode parent)
      {
        parent.SerializedData.IsEnabled = parent.IsChecked;
        CheckParent(parent);
      }
    }

    private void SaveNodeExpanded(List<TriggerTreeViewNode> nodes)
    {
      foreach (var node in nodes)
      {
        node.SerializedData.IsExpanded = node.IsExpanded;

        if (!node.IsTrigger && !node.IsOverlay)
        {
          SaveNodeExpanded(node.ChildNodes.Cast<TriggerTreeViewNode>().ToList());
        }
      }
    }

    private void ValueChanged(object sender, ValueChangedEventArgs args)
    {
      if (args.Property.Name != errorsItem.PropertyName && args.Property.Name != evalTimeItem.PropertyName &&
        args.Property.SelectedObject is TriggerPropertyModel trigger)
      {
        var list = thePropertyGrid.Properties.ToList();
        var errorsProp = PropertyGridUtil.FindProperty(list, errorsItem.PropertyName);
        var longestProp = PropertyGridUtil.FindProperty(list, evalTimeItem.PropertyName);

        bool isValid = true;
        if (trigger.UseRegex)
        {
          isValid = TestRegexProperty(trigger, trigger.Pattern, errorsProp);
        }

        if (isValid && trigger.EndUseRegex)
        {
          isValid = TestRegexProperty(trigger, trigger.CancelPattern, errorsProp);
        }

        if (isValid && trigger.Errors != "None")
        {
          trigger.Errors = "None";
          errorsProp.Value = "None";
          ErrorEditor.SetForeground("ContentForeground");
        }

        if (args.Property.Name == patternItem.PropertyName || args.Property.Name == useRegexItem.PropertyName)
        {
          trigger.LongestEvalTime = -1;
          longestProp.Value = -1;
        }
        else if (args.Property.Name == enableTimerItem.PropertyName)
        {
          PropertyGridUtil.EnableCategories(thePropertyGrid, new[] { new { Name = timerDurationItem.CategoryName, IsEnabled = (bool)args.Property.Value } });
        }

        saveButton.IsEnabled = (trigger.Errors == "None");
        cancelButton.IsEnabled = true;
      }
      else if (args.Property.SelectedObject is OverlayPropertyModel overlay)
      {
        var change = true;
        if (args.Property.Name == primaryBrushItem.PropertyName)
        {
          change = !overlay.PrimaryBrush.ToString().Equals(overlay.Original.PrimaryColor);
          Application.Current.Resources["TimerBarProgressColor-" + overlay.Id] = overlay.PrimaryBrush;
        }
        else if(args.Property.Name == secondaryBrushItem.PropertyName)
        {
          change = !overlay.SecondaryBrush.ToString().Equals(overlay.Original.SecondaryColor);
          Application.Current.Resources["TimerBarTrackColor-" + overlay.Id] = overlay.SecondaryBrush;
        }
        else if (args.Property.Name == fontBrushItem.PropertyName)
        {
          change = !overlay.FontBrush.ToString().Equals(overlay.Original.FontColor);
          Application.Current.Resources["TimerBarFontColor-" + overlay.Id] = overlay.FontBrush;
        }
        else if (args.Property.Name == fontSizeItem.PropertyName && overlay.FontSize.Split("pt") is string[] split && split.Length == 2
         && double.TryParse(split[0], out double newFontSize))
        {
          change = overlay.FontSize != overlay.Original.FontSize;
          Application.Current.Resources["TimerBarFontSize-" + overlay.Id] = newFontSize;
          Application.Current.Resources["TimerBarHeight-" + overlay.Id] = TriggerUtil.GetTimerBarHeight(newFontSize);
        }

        if (change)
        {
          saveButton.IsEnabled = true;
          cancelButton.IsEnabled = true;
        }
      }
    }

    private bool TestRegexProperty(Trigger trigger, string pattern, PropertyItem errorsProp)
    {
      bool isValid = TextFormatUtils.IsValidRegex(pattern);
      if (trigger.Errors == "None" && !isValid)
      {
        trigger.Errors = "Invalid Regex";
        errorsProp.Value = "Invalid Regex";
        ErrorEditor.SetForeground("EQWarnForegroundBrush");
      }

      return isValid;
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
      if (thePropertyGrid.SelectedObject is TriggerPropertyModel triggerModel)
      {
        TriggerUtil.Copy(triggerModel.Original, triggerModel);
        TriggerManager.Instance.UpdateTriggers();
      }
      else if (thePropertyGrid.SelectedObject is OverlayPropertyModel overlayModel)
      {
        TriggerUtil.Copy(overlayModel.Original, overlayModel);
        TriggerManager.Instance.UpdateOverlays();
      }

      cancelButton.IsEnabled = false;
      saveButton.IsEnabled = false;
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
      if (thePropertyGrid.SelectedObject is TriggerPropertyModel triggerModel)
      {
        TriggerUtil.Copy(triggerModel, triggerModel.Original);
        PropertyGridUtil.EnableCategories(thePropertyGrid, new[] { new { Name = timerDurationItem.CategoryName, IsEnabled = triggerModel.Original.EnableTimer } });
      }
      else if (thePropertyGrid.SelectedObject is OverlayPropertyModel overlayModel)
      {
        TriggerUtil.Copy(overlayModel, overlayModel.Original);
      }

      thePropertyGrid.RefreshPropertygrid();
      cancelButton.IsEnabled = false;
      saveButton.IsEnabled = false;
    }

    private TriggerTreeViewNode FindAndExpandNode(TriggerTreeViewNode node, object file)
    {
      if (node.SerializedData?.TriggerData == file || node.SerializedData?.OverlayData == file)
      {
        return node;
      }

      foreach (var child in node.ChildNodes.Cast<TriggerTreeViewNode>())
      {
        if (FindAndExpandNode(child, file) is TriggerTreeViewNode found)
        {
          treeView.ExpandNode(node);
          return found;
        }
      }

      return null;
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        (Application.Current.MainWindow as MainWindow).EventsLogLoadingComplete -= EventsLogLoadingComplete;
        TriggerManager.Instance.EventsUpdateTree -= EventsUpdateTriggerTree;
        TriggerManager.Instance.EventsSelectTrigger -= EventsSelectTrigger;
        treeView.DragDropController.Dispose();
        treeView.Dispose();
        TestSynth?.Dispose();
        disposedValue = true;
      }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      GC.SuppressFinalize(this);
    }
    #endregion
  }
}
