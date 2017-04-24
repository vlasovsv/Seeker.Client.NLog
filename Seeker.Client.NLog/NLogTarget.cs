using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace Seeker.Client.NLog
{
    /// <summary>
    /// Provides sending logs over HTTP to a Seeker server.
    /// </summary>
    [Target("Seeker")]
    public sealed class NLogTarget : Target
    {
        #region Private fields

        private readonly string _apiBatchResource = "api/v1/logs";

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an Nlog target to a Seeker server.
        /// </summary>
        public NLogTarget()
        {
            Properties = new List<SeekerPropertyItem>();
        } 

        #endregion

        #region Properties

        /// <summary>
        /// The address of the Seeker server to write to.
        /// </summary>
        [RequiredParameter]
        public Uri ServerUrl { get; set; }

        /// <summary>
        /// A list of properties that will be attached to the events.
        /// </summary>
        [ArrayParameter(typeof(SeekerPropertyItem), "property")]
        public IList<SeekerPropertyItem> Properties
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a log layout.
        /// </summary>
        private JsonLayout Layout
        {
            get;
            set;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the target.
        /// </summary>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            Layout = CreateLayout();
        }

        /// <summary>
        /// Creates a log layout.
        /// </summary>
        /// <returns>
        /// Returns a json layout that will be used to render logs.
        /// </returns>
        private JsonLayout CreateLayout()
        {
            var jsonLayout = new JsonLayout
            {
                Attributes =
                {
                    new JsonAttribute("timestamp", "${longdate}"),
                    new JsonAttribute("level", "${level:upperCase=true}"),
                    new JsonAttribute("message", "${message}"),
                    new JsonAttribute("exception", new JsonLayout
                    {
                        Attributes =
                        {
                            new JsonAttribute("type", "${exception:format=Type}"),
                            new JsonAttribute("message", "${exception:format=Message,Method,StackTrace}"),
                        },
                        RenderEmptyObject = false
                    },
                    false)
                }
            };

            if (Properties.Any())
            {
                var propertyLayout = new JsonLayout();
                foreach (var prop in Properties)
                {
                    propertyLayout.Attributes.Add(new JsonAttribute(prop.Name, prop.Value));
                }
                jsonLayout.Attributes.Add(new JsonAttribute("properties", propertyLayout, false));
            }

            return jsonLayout;
        }

        /// <summary>
        /// Writes an array of logging events to Seeker.
        /// </summary>
        /// <param name="logEvents">Logging events to be written.</param>
        protected override void Write(AsyncLogEventInfo[] logEvents)
        {
            var events = logEvents.Select(e => e.LogEvent);
            SendLogs(events);
        }

        /// <summary>
        /// Writes logging event to Seeker.
        /// </summary>
        /// <param name="logEvent">Logging event to be written.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            SendLog(logEvent);
        }

        /// <summary>
        /// Writes logging event to Seeker.
        /// </summary>
        /// <param name="logEvent">Logging event to be written.</param>
        protected override void Write(AsyncLogEventInfo logEvent)
        {
            SendLog(logEvent.LogEvent);
        }

        /// <summary>
        /// Creates a web request to Seeker.
        /// </summary>
        /// <returns>
        /// Returns a web request.
        /// </returns>
        private WebRequest CreateRequest()
        {
            var uri = new Uri(ServerUrl, _apiBatchResource);

            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";

            return request;
        }

        /// <summary>
        /// Sends a collection of logs to Seeker.
        /// </summary>
        /// <param name="logEvents">Logging events to be written.</param>
        void SendLogs(IEnumerable<LogEventInfo> logEvents)
        {
            if (ServerUrl == null)
            {
                return;
            }

            try
            {
                var request = CreateRequest();

                using (var requestStream = request.GetRequestStream())
                {
                    using (var payload = new StreamWriter(requestStream))
                    {
                        payload.Write("[");
                        var firstLog = logEvents.FirstOrDefault();
                        foreach (var logEvent in logEvents)
                        {
                            if (logEvent != firstLog)
                            {
                                payload.Write(",");
                            }
                            payload.Write(Layout.Render(logEvent));
                        }
                        payload.Write("]");
                    }
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    var responseStream = response.GetResponseStream();
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Sends a single log event to Seeker.
        /// </summary>
        /// <param name="logEvent">A log event to be written.</param>
        void SendLog(LogEventInfo logEvent)
        {
            if (ServerUrl == null)
            {
                return;
            }

            try
            {
                var request = CreateRequest();

                using (var requestStream = request.GetRequestStream())
                {
                    using (var payload = new StreamWriter(requestStream))
                    {
                        payload.Write(Layout.Render(logEvent));
                    }
                }
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    var responseStream = response.GetResponseStream();
                }
            }
            catch
            {
                return;
            }
        }
    }

    #endregion
}
