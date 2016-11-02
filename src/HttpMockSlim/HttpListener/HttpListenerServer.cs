﻿using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using HttpMockSlim.Model;

namespace HttpMockSlim.HttpListener
{
    public class HttpListenerServer : IHttpServer, IDisposable
    {
        #region Fields

        private System.Net.HttpListener _httpServer;
        private volatile bool _running = false;

        #endregion

        #region Properties

        public bool IsRunning => _running;

        #endregion

        #region Start/Stop

        public void Start(string uriPrefix, Action<Request, Response> sessionReceived)
        {


            _httpServer = new System.Net.HttpListener();
            _httpServer.Prefixes.Add(uriPrefix);
            _httpServer.Start();

            _running = true;

            _httpServer.BeginGetContext(HandleRequest, new SessionState(this, sessionReceived));
        }


        public void Stop()
        {
            if (!IsRunning)
                return;

            _running = false;

            _httpServer.Stop();
            //_httpServer = null;
        }

        #endregion

        #region Handle

        private static void HandleRequest(IAsyncResult result)
        {
            SessionState state = (SessionState) result.AsyncState;
            System.Net.HttpListener server = state.Server._httpServer;

            try
            {
                HttpListenerContext context = server.EndGetContext(result);
                server.BeginGetContext(HandleRequest, state);

                Task task = new Task(() => HandleSession(context, state), TaskCreationOptions.LongRunning);
                task.Start();
            }
            catch (Exception)
            {
                if (state.Server.IsRunning)
                    throw;
            }
            
        }

        private static void HandleSession(HttpListenerContext context, SessionState state)
        {
            Request request = MapRequest(context.Request);
            Response response = new Response();

            state.SessionReceived(request, response);

            WriteResponse(context.Response, response);
        }

        private static Request MapRequest(HttpListenerRequest clientRequest)
        {
            Request result = new Request
            {
                Method = clientRequest.HttpMethod,
                RawUrl = clientRequest.RawUrl
            };

            if (clientRequest.HasEntityBody)
            {
                using (Stream body = clientRequest.InputStream)  
                using (StreamReader reader = new StreamReader(body, clientRequest.ContentEncoding))
                {
                    result.Body = reader.ReadToEnd();
                }
            }

            return result;
        }

        private static void WriteResponse(HttpListenerResponse httpResponse, Response response)
        {
            httpResponse.StatusCode = response.StatusCode;
            httpResponse.ContentType = response.ContentType;

            httpResponse.ContentLength64 += response.Body.Length;

            response.Body.CopyTo(httpResponse.OutputStream);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Stop();
            _httpServer = null;
        }

        #endregion

        private class SessionState
        {
            public SessionState(HttpListenerServer server, Action<Request, Response> sessionReceived)
            {
                Server = server;
                SessionReceived = sessionReceived;
            }

            public readonly HttpListenerServer Server;
            public readonly Action<Request, Response> SessionReceived;
        }
    }
}