﻿using Syncfusion.Licensing;
using System.Windows;

namespace EQLogParser
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    public App()
    {
      SyncfusionLicenseProvider.RegisterLicense("LICENSE HERE");
    }
  }
}
