using JetBrains.Application;
using JetBrains.Application.Settings.Storage.DefaultFileStorages;
using JetBrains.DataFlow;
using JetBrains.Util;

namespace JetBox.Sync
{
  [ShellComponent]
  public class GlobalProductSyncSource : ISyncSource
  {
    public GlobalProductSyncSource(Lifetime lifetime, GlobalPerProductStorage globalPerProductStorage)
    {
      FilesToSync = new CollectionEvents<IProperty<FileSystemPath>>(lifetime, "GlobalProductSyncSource")
      {
        globalPerProductStorage.XmlFileStorage.Path
      };
    }

    public IViewable<IProperty<FileSystemPath>> FilesToSync { get; private set; }
  }
}