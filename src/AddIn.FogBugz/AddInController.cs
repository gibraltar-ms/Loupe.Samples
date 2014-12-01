﻿using System;
using System.Diagnostics;
using Gibraltar.AddIn.FogBugz.Internal;
using Gibraltar.Analyst.AddIn;
using Gibraltar.Analyst.Data;

namespace Gibraltar.AddIn.FogBugz
{
    /// <summary>
    /// The central controller for the entire set of FogBugz integration extensions
    /// </summary>
    [GibraltarAddIn("FogBugz", Description = "Integrates with FogBugz providing automatic defect management and lookup",
        ConfigurationEditor = typeof(ConfigurationEditor),
        CommonConfiguration = typeof(CommonConfig),
        UserConfiguration = typeof(UserConfig),
        HubConfiguration = typeof(HubConfig))]
    public class AddInController : IAddInController
    {
        public const string LogCategory = "AddIn.FogBugz";
        public const string ServerCredentialsKey = "FogBugz";

        private IAddInContext m_Context;
        private CommonConfig m_CommonConfig;
        private UserConfig m_UserConfig;
        private HubConfig m_HubConfig;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }

        /// <summary>
        /// Called to initialize the add in.
        /// </summary>
        /// <param name="context">A standard interface to the hosting environment for the add in, provided to all the different extensions that get loaded.  The controller
        ///             can keep this object for its entire lifecycle.</param><param name="controllerContext">Additional methods available to the repository controller during initialization.  This object should not be held between calls.</param>
        /// <remarks>
        /// <para>
        /// If any exception is thrown during this call this add in will not be loaded.
        /// </para>
        /// <para>
        /// During initialization the controller should register each of the other extension types that should be available to end users through appropriate calls to
        ///             the controllerContext.  These objects will be created and initialized as requred and provided the same IAddInContext object instance provided to this
        ///             method to enable coordination between all of the components.
        /// </para>
        /// </remarks>
        public void Initialize(IAddInContext context, IAddInControllerContext controllerContext)
        {
            //store off the context - we use it later if we get a config update event.
            m_Context = context;

            controllerContext.RegisterCommand(typeof(AddInCommand));
            controllerContext.RegisterSessionCommand(typeof(SessionAnalysisAddIn));
            controllerContext.RegisterSessionAnalyzer(typeof(SessionAnalysisAddIn));
            controllerContext.RegisterLogMessageCommand(typeof(AddDefectCommand));
            controllerContext.RegisterSessionSummaryView(typeof(FogBugzSummaryView), "FogBugz Case List", "Find sessions associated with FogBugz cases", null);
            controllerContext.RegisterSessionSummaryView(typeof(FogBugzLookupView), "FogBugz Lookup", "Find sessions associated with a FogBugz Case Id", null);

            //and load up our initial configuration
            ConfigurationChanged();
        }

        /// <summary>
        /// Called by Gibraltar to indicate the configuration of the add in has changed at runtime
        /// </summary>
        public void ConfigurationChanged()
        {
            m_CommonConfig = (CommonConfig)m_Context.Configuration.Common;
            m_UserConfig = (UserConfig)m_Context.Configuration.User;
            m_HubConfig = (HubConfig)m_Context.Configuration.Hub;
        }


        /// <summary>
        /// Returns the MappingTarget appropriate for a session, if any.
        /// Otherwise, return null.
        /// </summary>
        public Mapping FindTarget(ISession session)
        {
            // Throw an exception if we can't find or parse the config file
            if (m_CommonConfig == null)
                throw new ApplicationException("Could not find or parse configuration");

            string product = session.Summary.Product;
            string application = session.Summary.Application;
            string version = session.Summary.ApplicationVersion.ToString();

            return m_CommonConfig.Mappings.FindMatch(product, application, version);
        }

        /// <summary>
        /// Our common FogBugz configuration.
        /// </summary>
        public CommonConfig CommonConfiguration { get { return m_CommonConfig; } }

        /// <summary>
        /// Our hub FogBugz configuration.  May be null if no hub is configured
        /// </summary>
        public HubConfig HubConfiguration { get { return m_HubConfig; } }

        /// <summary>
        /// Our user FogBugz configuration.  May be null if executing on Hub.
        /// </summary>
        public UserConfig UserConfiguration { get { return m_UserConfig; } }

        /// <summary>
        /// Create a new web browser to open the selected case.
        /// </summary>
        /// <param name="caseNumber"></param>
        public void WebSiteOpenCase(int caseNumber)
        {
            if (caseNumber > 0)
            {
                string url = string.Format("{0}?{1}", m_CommonConfig.BaseUrl, caseNumber);
                Process.Start(url);
            }
        }

        /// <summary>
        /// Create a new web browser to open the FogBugz server with the url
        /// </summary>
        /// <param name="pathAndArgs"></param>
        public void WebSiteOpen(string pathAndArgs)
        {
            string url = string.Format("{0}/{1}", m_CommonConfig.BaseUrl, pathAndArgs);
            Process.Start(url);
        }

        /// <summary>
        /// The filter query string in FogBugz format
        /// </summary>
        public string Filter
        {
            get
            {
                string queryString = string.Empty;

                switch (m_UserConfig.CaseStatusFilter)
                {
                    case CaseStatus.Active:
                        queryString += "status:Active ";
                        break;
                    case CaseStatus.Resolved:
                        queryString += "status:Resolved ";
                        break;
                    case CaseStatus.Closed:
                        queryString += "status:Closed ";
                        break;
                    case CaseStatus.All:
                        //no filter
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (m_UserConfig.LastUpdatedFilter)
                {
                    case LastUpdatedFilter.None:
                        //no filter
                        break;
                    case LastUpdatedFilter.OneYear:
                        queryString += "edited:\"-365d..\"";
                        break;
                    case LastUpdatedFilter.ThreeMonths:
                        queryString += "edited:\"-93d..\"";
                        break;
                    case LastUpdatedFilter.OneMonth:
                        queryString += "edited:\"-31d..\"";
                        break;
                    case LastUpdatedFilter.OneWeek:
                        queryString += "edited:\"-7d..\"";
                        break;
                    case LastUpdatedFilter.OneDay:
                        queryString += "edited:\"yesterday..\"";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return queryString;
            }
        }

        /// <summary>
        /// A display caption for the current filter
        /// </summary>
        public string FilterCaption
        {
            get
            {
                string caption = string.Format("{0} cases", m_UserConfig.CaseStatusFilter);

                if (m_UserConfig.LastUpdatedFilter != LastUpdatedFilter.None)
                {
                    caption += " updated within";
                    switch (m_UserConfig.LastUpdatedFilter)
                    {
                        case LastUpdatedFilter.OneYear:
                            caption += " one year";
                            break;
                        case LastUpdatedFilter.ThreeMonths:
                            caption += " three months";
                            break;
                        case LastUpdatedFilter.OneMonth:
                            caption += " one month";
                            break;
                        case LastUpdatedFilter.OneWeek:
                            caption += " one week";
                            break;
                        case LastUpdatedFilter.OneDay:
                            caption += " one day";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return caption;
            }
        }

        internal FBApi GetApi()
        {
            string url = m_CommonConfig.Url;
            string userName, password;
            if (m_Context.Environment == GibraltarEnvironment.HubServer)
            {
                m_Context.Configuration.GetHubCredentials(ServerCredentialsKey, out userName, out password);
            }
            else
            {
                m_Context.Configuration.GetUserCredentials(ServerCredentialsKey, out userName, out password);
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new InvalidOperationException("No FogBugz server url has been configured to connect to.");
            }

            if (string.IsNullOrEmpty(userName))
            {
                throw new InvalidOperationException(string.Format("No account has been provided to connect to FogBugz with.  Please configure a {0} account.",
                    (m_Context.Environment == GibraltarEnvironment.HubServer) ? "hub" : "personal"));
            }

            // Throw an exception if we can't connect with FogBugz
            FBApi api;
            try
            {
                m_Context.Log.Verbose(LogCategory, "Connecting to FogBugz Server", "Server Url: {0}\r\nUser Name: {1}", url, userName);
                api = new FBApi(url, userName, password);
            }
            catch (Exception ex)
            {
                m_Context.Log.Error(ex, LogCategory, "Failed to connect to FogBugz Server", "Server Url: {0}\r\nUser Name: {1}\r\nException: {2}", url, userName, ex.Message);
                throw new ApplicationException("Could not contact FogBugz Server", ex);
            }

            return api;
        }

    }
}
