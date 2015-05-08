using System.Net;
using DropNet;
using DropNet.Exceptions;
using JetBrains.Util;

namespace JetBox.Dropbox
{
  public class Client : DropNetClient
  {
    private readonly ILogger myLogger;

    public Client(ILogger logger, string apiKey, string appSecret, IWebProxy webProxy) : base(apiKey, appSecret, webProxy)
    {
      myLogger = logger;
    }

    public void LogException(DropboxException exception)
    {
      if ((exception is DropboxRestException) && ((DropboxRestException) exception).StatusCode == HttpStatusCode.Unauthorized)
        return;

        myLogger.LogExceptionSilently(exception);
    }
  }
}