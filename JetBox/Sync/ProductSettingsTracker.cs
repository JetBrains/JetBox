using System.IO;
using System.Net;
using JetBox.Dropbox;
using JetBox.Options;
using JetBrains.Application;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Storage.DefaultFileStorages;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBox.Sync
{
  [ShellComponent]
  public class ProductSettingsTracker
  {
    private readonly ClientFactory myClientFactory;
    private readonly FileSystemPath myRootFolder;
    private readonly IContextBoundSettingsStoreLive mySettingsStore;
    private Client myClient;

    public ProductSettingsTracker(Lifetime lifetime, IProductNameAndVersion product, ClientFactory clientFactory, GlobalPerProductStorage globalPerProductStorage, IFileSystemTracker fileSystemTracker, JetBoxSettingsStorage jetBoxSettings)
    {
      myClientFactory = clientFactory;
      mySettingsStore = jetBoxSettings.SettingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
      mySettingsStore.Changed.Advise(lifetime, _ => InitClient());

      myRootFolder = FileSystemPath.Parse(product.ProductName);
      InitClient();

      var productSettingsPath = globalPerProductStorage.XmlFileStorage.Path;

      SyncFromCloud(productSettingsPath.Value);

      var fileTrackingLifetime = new SequentialLifetimes(lifetime);
      productSettingsPath.Change.Advise(lifetime, args => fileTrackingLifetime.Next(lt =>
      {
        if (args.HasNew)
        {
          fileSystemTracker.AdviseFileChanges(lt, args.New, delta => delta.Accept(new FileChangesVisitor(myClient, myRootFolder)));
        }
      }));
    }

    public class FileChangesVisitor : IFileSystemChangeDeltaVisitor
    {
      private readonly Client myClient;
      private readonly FileSystemPath myRootFolder;

      public FileChangesVisitor(Client client, FileSystemPath rootFolder)
      {
        myClient = client;
        myRootFolder = rootFolder;
      }

      public void Visit(FileSystemChangeDelta delta)
      {
        switch (delta.ChangeType)
        {
          case FileSystemChangeType.ADDED:
          case FileSystemChangeType.CHANGED:
            Upload(delta.NewPath);
            break;
          case FileSystemChangeType.DELETED:
            Delete(delta.OldPath);
            break;
          case FileSystemChangeType.RENAMED:
            Rename(delta.OldPath, delta.NewPath);
            break;
          case FileSystemChangeType.SUBTREE_CHANGED:
            foreach (var changeDelta in delta.GetChildren())
            {
              changeDelta.Accept(this);
            }
            break;
        }
      }

      private void Rename(FileSystemPath oldLocalPath, FileSystemPath newLocalPath)
      {
        myClient.MoveAsync(GetRemotePath(oldLocalPath), GetRemotePath(newLocalPath),
          response => { },
          myClient.LogException);
      }

      private void Upload(FileSystemPath localPath)
      {
        myClient.UploadFileAsync(GetRemotePath(localPath).TrimFromEnd(localPath.Name), new FileInfo(localPath.FullPath),
          data => { },
          myClient.LogException);
      }

      private void Delete(FileSystemPath localPath)
      {
        myClient.DeleteAsync(GetRemotePath(localPath),
          response => { },
          myClient.LogException);
      }

      private string GetRemotePath(FileSystemPath localPath)
      {
        return myRootFolder.Combine(localPath.Name).FullPath.Replace('\\', '/');
      }
    }

    private void InitClient()
    {
      myClient = myClientFactory.CreateClient();
      myClient.UserLogin = mySettingsStore.GetValue(JetBoxSettingsAccessor.Login);
    }

    private void SyncFromCloud(FileSystemPath localPath)
    {
      var remotePath = myRootFolder.Combine(localPath.Name).FullPath.Replace('\\', '/');
      myClient.GetFileAsync(remotePath,
        response =>
        {
          if (response.StatusCode == HttpStatusCode.OK)
          {
            localPath.WriteAllBytes(response.RawBytes);
          }
        },
        exception =>
        {
          if (exception.StatusCode == HttpStatusCode.NotFound)
          {
            myClient.UploadFileAsync(myRootFolder.FullPath.Replace('\\', '/'), new FileInfo(localPath.FullPath),
              data => { },
              myClient.LogException);
            return;
          }
          myClient.LogException(exception);
        });
    }
  }
}