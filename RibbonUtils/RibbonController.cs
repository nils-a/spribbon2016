﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RibbonUtils.Definitions;
using System.Web.UI;
using Microsoft.SharePoint.WebControls;
using System.Xml;
using Microsoft.Web.CommandUI;
using RibbonUtils.Definitions.Controls;
using Microsoft.SharePoint;
using System.Runtime.InteropServices;

namespace RibbonUtils
{
    /// <summary>
    /// Manage the ribbon creation from page (local ribbon)
    /// </summary>
    internal class RibbonController
    {
        #region Singleton

        private static RibbonController instance = null;

        /// <summary>
        /// Singleton instance of RibbonController class
        /// </summary>
        public static RibbonController Current
        {
            get
            {
                if (instance == null)
                    instance = new RibbonController();

                return instance;
            }
        }

        private RibbonController()
        {
        }

        #endregion

        #region Internal

        internal void AddRibbonContextualTabToPage(ContextualGroupDefinition definition, Page page)
        {
            page.PreRenderComplete += new EventHandler(page_PreRenderComplete);

            AddRibbonExtension(XmlGenerator.Current.GetContextualGroupXML(definition), page, "Ribbon.ContextualTabs");
            AddGroupTemplatesRibbonExtensions(definition.Tabs.SelectMany(t => t.GroupTemplates), page);

            RibbonCommandRepository.Current.AddCommands(definition);
        }

        internal void AddRibbonTabToPage(TabDefinition definition, Page page)
        {
            AddRibbonExtension(XmlGenerator.Current.GetTabXML(definition), page, "Ribbon.Tabs");
            AddGroupTemplatesRibbonExtensions(definition.GroupTemplates, page);

            RibbonCommandRepository.Current.AddCommands(definition);
            RegisterCommands(page);

            Ribbon ribbon = SPRibbon.GetCurrent(page);
            ribbon.MakeTabAvailable("Ribbon." + definition.Id);
            ribbon.InitialTabId = "Ribbon." + definition.Id;
        }

        #endregion

        #region Private functions

        private void page_PreRenderComplete(object sender, EventArgs e)
        {
            Page page = sender as Page;

            if (RibbonCommandRepository.Current.GetCommandsCount() > 0)
                RegisterCommands(page);
        }

        private void AddGroupTemplatesRibbonExtensions(IEnumerable<GroupTemplateDefinition> templates, Page page)
        {
            Ribbon ribbon = SPRibbon.GetCurrent(page);
            XmlDocument ribbonExtensions = new XmlDocument();

            foreach (GroupTemplateDefinition template in templates)
            {
                ribbonExtensions.LoadXml(template.XML);
                ribbon.RegisterDataExtension(ribbonExtensions.FirstChild, "Ribbon.Templates._children");
            }
        }

        private void AddRibbonExtension(string xml, Page page, string parentId)
        {
            Ribbon ribbon = SPRibbon.GetCurrent(page);
            ribbon.Minimized = false;
            ribbon.CommandUIVisible = true;
            XmlDocument ribbonExtensions = new XmlDocument();
            ribbonExtensions.LoadXml(xml);
            ribbon.RegisterDataExtension(ribbonExtensions.FirstChild, parentId + "._children");
        }

        private void RegisterCommands(Page page)
        {
            SPRibbonScriptManager ribbonScriptManager = new SPRibbonScriptManager();

            ribbonScriptManager.RegisterGetCommandsFunction(page, "getGlobalCommands", RibbonCommandRepository.Current.GetCommands());
            ribbonScriptManager.RegisterCommandEnabledFunction(page, "commandEnabled", RibbonCommandRepository.Current.GetCommands());
            ribbonScriptManager.RegisterHandleCommandFunction(page, "handleCommand", RibbonCommandRepository.Current.GetCommands());

            page.ClientScript.RegisterClientScriptBlock(
                page.GetType(),
                "InitPageComponent",
                PageComponentScript.GetText("RibbonUtils"));

            RibbonCommandRepository.Current.ClearCommands();
        }

        #endregion

    }
}
