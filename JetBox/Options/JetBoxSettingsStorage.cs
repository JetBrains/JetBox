using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.Env.Components;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Logging;
using JetBrains.Application.Settings.Storage;
using JetBrains.Application.Settings.Storage.Persistence;
using JetBrains.Application.Settings.Store.Implementation;
using JetBrains.DataFlow;
using JetBrains.Threading;
using JetBrains.Util;

namespace JetBox.Options
{
  /// <summary>
  /// Store JetBox settings separately to do not sync itself
  /// </summary>
  [ShellComponent]
  public class JetBoxSettingsStorage
  {
    private readonly SettingsStore mySettingsStore;
    public ISettingsStore SettingsStore { get { return mySettingsStore; } }

    private class JetBoxSettingsProvider : FileSettingsStorageProviderBase
    {
      public JetBoxSettingsProvider([NotNull] Lifetime lifetime, [NotNull] string name, [NotNull] IProperty<FileSystemPath> path, bool isWritable, double priority, [NotNull] IIsAvailable isAvailable, SettingsStoreSerializationToXmlDiskFile.SavingEmptyContent whenNoContent, [NotNull] IThreading threading, [NotNull] IFileSystemTracker filetracker,
#if RS_80
        [NotNull] FileSettingsStorageBehavior behavior,
#else
        [NotNull] IFileSettingsStorageBehavior behavior, InternKeyPathComponent interned,
#endif
        IEnumerable<KeyValuePair<PropertyId, object>> metadata)
        : base(lifetime, name, path, isWritable, priority, isAvailable, whenNoContent, threading, filetracker, behavior, 
#if RS_80
#else
                interned,
#endif
        metadata)
      {}
    }
#if RS_80
    public JetBoxSettingsStorage(Lifetime lifetime, ProductSettingsLocation productSettingsLocation, ISettingsSchema settingsSchema, DataContexts dataContexts, IThreading threading, IFileSystemTracker fileSystemTracker, FileSettingsStorageBehavior settingsStorageBehavior, ISettingsLogger settingsLogger, ISettingsChangeDispatch settingsChangeDispatch, SettingsStorageMountPoints.SelfCheckControl selfCheckControl)
#else
    public JetBoxSettingsStorage(Lifetime lifetime, ProductSettingsLocation productSettingsLocation, ISettingsSchema settingsSchema, DataContexts dataContexts, IThreading threading, IFileSystemTracker fileSystemTracker, IFileSettingsStorageBehavior settingsStorageBehavior, ISettingsLogger settingsLogger, ISettingsChangeDispatch settingsChangeDispatch, SettingsStorageMountPoints.SelfCheckControl selfCheckControl, InternKeyPathComponent interned)
#endif
    {
      var filePath = productSettingsLocation.GetUserSettingsNonRoamingDir(ProductSettingsLocationFlag.ThisProductThisVersionThisEnvironment).Combine("JetBox" + XmlFileSettingsStorage.SettingsStorageFileExtensionWithDot);
      var property = new Property<FileSystemPath>(lifetime, GetType().Name + "Path", filePath);
      var settingsProvider = new JetBoxSettingsProvider(lifetime, GetType().Name + "::Provider", property, true, 0, IsAvailable.Always, SettingsStoreSerializationToXmlDiskFile.SavingEmptyContent.DeleteFile, threading, fileSystemTracker, settingsStorageBehavior,
#if RS_80
#else
                interned,
#endif
      new Dictionary<PropertyId, object>());
      var mounts = new SettingsStorageMountPoints(lifetime,
        new CollectionEvents<IProvider<ISettingsStorageMountPoint>>(lifetime, GetType() + "::Mounts") { settingsProvider }, threading, settingsLogger,
        selfCheckControl);

      mySettingsStore = new SettingsStore(lifetime, settingsSchema, mounts, dataContexts, null, settingsLogger, settingsChangeDispatch );
    }
  }
}