using System.Net;
using DropNet;
using DropNet.Exceptions;
using JetBrains.Util;

namespace JetBox.Dropbox
{
  public class Client : DropNetClient
  {
    private readonly ILogger myLogger;

    public Client(ILogger logger, string apiKey, string appSecret) : base(apiKey, appSecret)
    {
      myLogger = logger;
    }

    public void LogException(DropboxException exception)
    {
      switch (exception.StatusCode)
      {
        case HttpStatusCode.Unauthorized:
          break;

        default:
          myLogger.LogExceptionSilently(exception);
          break;
      }
    }
  }
}