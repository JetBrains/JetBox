using System;
using JetBrains.Application;
using JetBrains.Application.Env;
using JetBrains.Application.Env.Components;
using JetBrains.Application.Extensions;
using JetBrains.DataFlow;
using JetBrains.Util;

namespace JetBox.Sync
{
  [EnvironmentComponent(Sharing.Product)]
  public class ExtensionsSyncSource : ISyncSource
  {
    public ExtensionsSyncSource(Lifetime lifetime, ExtensionLocations extensionLocations, IProductNameAndVersion product, AnyProductSettingsLocation anyProductSettingsLocation)
    {
      FilesToSync = new CollectionEvents<IProperty<FileSystemPath>>(lifetime, "ExtensionsSyncSource")
      {
        new Property<FileSystemPath>(lifetime, "ExtensionsSyncSource::ExtensionsSettings1", extensionLocations.UserExtensionSettingsFilePath),
        new Property<FileSystemPath>(lifetime, "ExtensionsSyncSource::ExtensionsSettings2", extensionLocations.UserExtensionSettingsFilePath.ChangeExtension(".xml")),
        new Property<FileSystemPath>(lifetime, "ExtensionsSyncSource::NuGetPackages.config",
          extensionLocations.GetBaseLocation(Environment.SpecialFolder.ApplicationData, anyProductSettingsLocation, product).Combine("packages.config"))
      };
    }

    public IViewable<IProperty<FileSystemPath>> FilesToSync { get; private set; }
  }
}