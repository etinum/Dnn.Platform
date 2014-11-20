﻿#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2014
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Content.Workflow;
using DotNetNuke.Entities.Content.Workflow.Entities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Framework;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Exceptions;

namespace DotNetNuke.Entities.Tabs
{
    class TabWorkflowTracker : ServiceLocator<ITabChangeTracker, TabWorkflowTracker>, ITabChangeTracker
    {
        private static readonly DnnLogger Logger = DnnLogger.GetClassLogger(typeof(TabWorkflowTracker));
       
        #region Members
        private readonly ITabController _tabController;
        private readonly IWorkflowEngine _workflowEngine;
        private readonly IWorkflowManager _workflowManager;
        private readonly ITabWorkflowSettings _tabWorkflowSettings;
        #endregion

        public TabWorkflowTracker()
        {
            _tabController = TabController.Instance;
            _workflowEngine = WorkflowEngine.Instance;
            _workflowManager = WorkflowManager.Instance;
            _tabWorkflowSettings = TabWorkflowSettings.Instance;
        }

        protected override Func<ITabChangeTracker> GetFactory()
        {
            return () => new TabWorkflowTracker();
        }

        public void TrackModuleAddition(ModuleInfo module, int moduleVersion, int userId)
        {
            NotifyWorkflowAboutChanges(module.PortalID, module.TabID, userId);
        }

        public void TrackModuleModification(ModuleInfo module, int moduleVersion, int userId)
        {
            NotifyWorkflowAboutChanges(module.PortalID, module.TabID, userId);
        }

        public void TrackModuleDeletion(ModuleInfo module, int moduleVersion, int userId)
        {
            NotifyWorkflowAboutChanges(module.PortalID, module.TabID, userId);
        }

        #region Private Statics Methods
        private void NotifyWorkflowAboutChanges(int portalId, int tabId, int userId)
        {
            try
            {
                var tabInfo = _tabController.GetTab(tabId, portalId);
                if (_workflowEngine.IsWorkflowCompleted(tabInfo))
                {
                    var workflow = GetCurrentOrDefaultWorkflow(tabInfo, portalId);
                    if (workflow == null)
                    {
                        Logger.Warn("Current Workflow and Default workflow are not found on NotifyWorkflowAboutChanges");
                        return;
                    }

                    _workflowEngine.StartWorkflow(workflow.WorkflowID, tabInfo.ContentItemId, userId);
                    _tabController.ClearCache(portalId);
                }
            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);
            }
            
        }

        private Workflow GetCurrentOrDefaultWorkflow(ContentItem item, int portalId)
        {
            if (item.StateID != Null.NullInteger)
            {
                return _workflowManager.GetWorkflow(item);
            }

            var defaultWorkflow = _tabWorkflowSettings.GetDefaultTabWorkflowId(portalId);
            return _workflowManager.GetWorkflow(defaultWorkflow);
        }
        #endregion
    }
}
