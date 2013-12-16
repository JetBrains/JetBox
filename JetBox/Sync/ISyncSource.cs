using JetBrains.DataFlow;
using JetBrains.Util;

namespace JetBox.Sync
{
  public interface ISyncSource
  {
    IViewable<IProperty<FileSystemPath>> FilesToSync { get; }
  }
}