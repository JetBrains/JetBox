using System.Net;
using DropNet;
using DropNet.Exceptions;
using JetBrains.Util;

namespace JetBox.Dropbox
{
  public class Client : DropNetClient
  {
    public ILogger Logger { get; private set; }

    public Client(ILogger logger, string apiKey, string appSecret, IWebProxy webProxy) : base(apiKey, appSecret, webProxy)
    {
      Logger = logger;
    }

    public void LogException(DropboxException exception)
    {
      if ((exception is DropboxRestException) && ((DropboxRestException) exception).StatusCode == HttpStatusCode.Unauthorized)
        return;

        Logger.LogExceptionSilently(exception);
    }
  }
}