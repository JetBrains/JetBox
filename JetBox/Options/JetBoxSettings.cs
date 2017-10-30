using System;
using System.Linq.Expressions;
using DropNet.Models;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.WellKnownRootKeys;

namespace JetBox.Options
{
  [SettingsKey(typeof(HousekeepingSettings), "JetBox settings")]
  public class JetBoxSettings
  {
    [SettingsEntry(null, "Dropbox user login", ValueSerializer = SettingsStoreSerializerType.XmlSerializer)]
    public UserLogin Login { get; set; }
  }

  public static class JetBoxSettingsAccessor
  {
    public static readonly Expression<Func<JetBoxSettings, UserLogin>> Login = key => key.Login;
  }
}