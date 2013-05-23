using System.Linq;
using System.Net;
using DropNet;
using DropNet.Exceptions;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBox.Dropbox
{
  // TODO: parameter are hardcoded
  public class Client : DropNetClient
  {
    public static string RootName = "JetBox";
    
    public Client() : base("4yt9baw2q6mzdaq", "e16o5uqliplbkvt")
    {
      UseSandbox = true;
    }

    public void LogException(DropboxException exception)
    {
      switch (exception.StatusCode)
      {
        case HttpStatusCode.Unauthorized:
          break;

        default:
          Logger.LogExceptionSilently(exception);
          break;
      }
    }
  }
}