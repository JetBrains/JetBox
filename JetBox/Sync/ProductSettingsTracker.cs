using System.IO;
using System.Net;
using JetBox.Dropbox;
using JetBox.Options;
using JetBrains.Application;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Settings;
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

    public ProductSettingsTracker(Lifetime lifetime, IProductNameAndVersion product, ClientFactory clientFactory, IViewable<ISyncSource> syncSources, IFileSystemTracker fileSystemTracker, JetBoxSettingsStorage jetBoxSettings)
    {
      myClientFactory = clientFactory;
      mySettingsStore = jetBoxSettings.SettingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
      mySettingsStore.Changed.Advise(lifetime, _ => InitClient());

      myRootFolder = FileSystemPath.Parse(product.ProductName);
      InitClient();

      syncSources.View(lifetime, (lt1, source) =>
        source.FilesToSync.View(lt1, (lt2, fileToSync) =>
        {
          SyncFromCloud(fileToSync.Value);

          var fileTrackingLifetime = new SequentialLifetimes(lt2);
          fileToSync.Change.Advise(lt2,
            args =>
            {
              var path = args.Property.Value;
              if (lifetime.IsTerminated || path.IsNullOrEmpty())
              {
                fileTrackingLifetime.TerminateCurrent();
              }
              else
              {
                fileTrackingLifetime.Next(lt =>
                    fileSystemTracker.AdviseFileChanges(lt, path,
                      delta => delta.Accept(new FileChangesVisitor(myClient, myRootFolder))));
              }
            });
        }));
    }

    private class FileChangesVisitor : IFileSystemChangeDeltaVisitor
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