﻿using System.Windows;
using System.Windows.Controls;

namespace EQLogParser
{
  class UIElementUtil
  {
    internal static void CheckHideTitlePanel(Panel titlePanel, Panel optionsPanel)
    {
      var settingsLoc = optionsPanel.PointToScreen(new Point(0, 0));
      var titleLoc = titlePanel.PointToScreen(new Point(0, 0));

      if ((titleLoc.X + titlePanel.ActualWidth) > (settingsLoc.X + 10))
      {
        titlePanel.Visibility = Visibility.Hidden;
      }
      else
      {
        titlePanel.Visibility = Visibility.Visible;
      }
    }

    internal static void SetComboBoxTitle(ComboBox columns, int count, string value)
    {
      if (!(columns.SelectedItem is ComboBoxItemDetails selected))
      {
        selected = columns.Items[0] as ComboBoxItemDetails;
      }

      string countString = columns.Items.Count == count ? "All" : count.ToString();
      selected.SelectedText = countString + " " + value;
      columns.SelectedIndex = -1;
      columns.SelectedItem = selected;
    }

    internal static void SetSize(FrameworkElement element, double height, double width)
    {
      if (!double.IsNaN(height) && element.Height != height)
      {
        element.Height = height;
      }

      if (!double.IsNaN(width) && element.Width != width)
      {
        element.Width = width;
      }
    }
  }
}
