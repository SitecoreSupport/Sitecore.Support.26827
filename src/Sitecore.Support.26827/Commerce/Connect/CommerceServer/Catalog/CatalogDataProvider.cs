using CommerceServer.Core.Catalog;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.Items;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using System;
using Sitecore.Commerce.Connect.CommerceServer;
using Sitecore.Commerce.Connect.CommerceServer.Catalog;

namespace Sitecore.Support.Commerce.Connect.CommerceServer.Catalog
{
  /// <summary>
  /// Write enabled Sitecore/Commerce Server Catalog Provider.
  /// </summary>
  public class CatalogDataProvider : ReadOnlyCatalogDataProvider
  {
    private object _templateInitializationLock = new object();
    private bool _templatesInitializing;
    private bool _templatesInitialized;

    /// <summary>Saves an item.</summary>
    /// <param name="itemDefinition">The item definition.</param>
    /// <param name="changes">The changes.</param>
    /// <param name="context">The context.</param>
    /// <returns>true if we have processed the item; otherwise false.</returns>
    public override bool SaveItem(ItemDefinition itemDefinition, ItemChanges changes, CallContext context)
    {
      if (!CommerceUtility.IsCommerceServerInitialized)
        return false;
      ExternalIdInformation information = (ExternalIdInformation)null;
      if (this.CanProcessCatalogItem(itemDefinition.ID, context, out information))
      {
        ISitecoreDataProvider catalogDataProvider = this.GetCatalogDataProvider(information.CommerceItemType);
        if (catalogDataProvider != null)
          return catalogDataProvider.SaveItem((DataProvider)this, itemDefinition, changes, context, information);
      }
      return false;
    }

    /// <summary>Creates an item.</summary>
    /// <param name="itemID">The item ID.</param>
    /// <param name="itemName">Name of the item.</param>
    /// <param name="templateID">The template ID.</param>
    /// <param name="parent">The parent.</param>
    /// <param name="context">The context.</param>
    /// <returns>true if we have processed the item; otherwise false.</returns>
    public override bool CreateItem(ID itemID, string itemName, ID templateID, ItemDefinition parent, CallContext context)
    {
      if (itemName == "__Standard Values" || !CommerceUtility.IsCommerceServerInitialized)
        return false;
      ISitecoreDataProvider catalogDataProvider = this.GetCatalogDataProvider(templateID, context);
      if (catalogDataProvider == null)
        return false;
      int num = catalogDataProvider.CreateItem((DataProvider)this, itemID, itemName, templateID, parent, context) ? 1 : 0;
      if (num == 0)
        return num != 0;
      CatalogUtility.AddToExternalIdCache(itemID.Guid, true);
      return num != 0;
    }

    /// <summary>Creates an item.</summary>
    /// <param name="itemID">The item ID.</param>
    /// <param name="itemName">Name of the item.</param>
    /// <param name="templateID">The template ID.</param>
    /// <param name="parent">The parent.</param>
    /// <param name="created">The date created.</param>
    /// <param name="context">The context.</param>
    /// <returns>true if we have processed the item; otherwise false.</returns>
    public override bool CreateItem(ID itemID, string itemName, ID templateID, ItemDefinition parent, DateTime created, CallContext context)
    {
      return this.CreateItem(itemID, itemName, templateID, parent, context);
    }

    /// <summary>Deletes an item.</summary>
    /// <param name="itemDefinition">The item definition.</param>
    /// <param name="context">The context.</param>
    /// <returns>true if we have processed the item; otherwise false.</returns>
    public override bool DeleteItem(ItemDefinition itemDefinition, CallContext context)
    {
      if (!CommerceUtility.IsCommerceServerInitialized)
        return false;
      ExternalIdInformation information = (ExternalIdInformation)null;
      if (this.CanProcessCatalogItem(itemDefinition.ID, context, out information))
      {
        ISitecoreDataProvider catalogDataProvider = this.GetCatalogDataProvider(information.CommerceItemType);
        if (catalogDataProvider != null)
        {
          int num = catalogDataProvider.DeleteItem((DataProvider)this, itemDefinition, context, information) ? 1 : 0;
          if (num == 0)
            return num != 0;
          CatalogUtility.RemoveFromExternalIdCache(information.ExternalId, true);
          return num != 0;
        }
      }
      return false;
    }

    /// <summary>Moves an item (ie. changes its parent).</summary>
    /// <param name="itemDefinition">The item definition.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="context">The call context.</param>
    /// <returns>
    ///   <c>True</c> if the item was moved by the provider, <c>false</c> otherwise.
    /// </returns>
    public override bool MoveItem(ItemDefinition itemDefinition, ItemDefinition destination, CallContext context)
    {
      if (!CommerceUtility.IsCommerceServerInitialized)
        return false;
      ExternalIdInformation information = (ExternalIdInformation)null;
      if (this.CanProcessCatalogItem(itemDefinition.ID, context, out information))
      {
        ISitecoreDataProvider catalogDataProvider = this.GetCatalogDataProvider(itemDefinition.TemplateID, context);
        if (catalogDataProvider != null)
          return catalogDataProvider.MoveItem((DataProvider)this, itemDefinition, destination, context, information);
      }
      return base.MoveItem(itemDefinition, destination, context);
    }

    /// <summary>Copies an item.</summary>
    /// <param name="source">The source.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="copyName">Name of the copy.</param>
    /// <param name="copyID">The copy ID.</param>
    /// <param name="context">The context.</param>
    /// <returns>true if we have processed the item; otherwise false.</returns>
    public override bool CopyItem(ItemDefinition source, ItemDefinition destination, string copyName, ID copyID, CallContext context)
    {
      if (!CommerceUtility.IsCommerceServerInitialized)
        return false;
      ExternalIdInformation information = (ExternalIdInformation)null;
      if (this.CanProcessCatalogItem(source.ID, context, out information))
      {
        ISitecoreDataProvider catalogDataProvider = this.GetCatalogDataProvider(information.CommerceItemType);
        if (catalogDataProvider != null)
          return catalogDataProvider.CopyItem((DataProvider)this, source, destination, copyName, copyID, context, information);
      }
      return base.CopyItem(source, destination, copyName, copyID, context);
    }

    /// <summary>Adds the version.</summary>
    /// <param name="itemDefinition">The item definition.</param>
    /// <param name="baseVersion">The base version.</param>
    /// <param name="context">The context.</param>
    /// <returns>The version that was added.  -1 if not version was added.</returns>
    public override int AddVersion(ItemDefinition itemDefinition, VersionUri baseVersion, CallContext context)
    {
      if (!CommerceUtility.IsCommerceServerInitialized)
        return -1;
      ExternalIdInformation information = (ExternalIdInformation)null;
      if (this.CanProcessCatalogItem(itemDefinition.ID, context, out information))
      {
        ISitecoreDataProvider catalogDataProvider = this.GetCatalogDataProvider(itemDefinition.TemplateID, context);
        if (catalogDataProvider != null)
          return catalogDataProvider.AddVersion((DataProvider)this, itemDefinition, baseVersion, context, information);
      }
      return base.AddVersion(itemDefinition, baseVersion, context);
    }

    /// <summary>Removes the version.</summary>
    /// <param name="itemDefinition">The item definition.</param>
    /// <param name="version">The version.</param>
    /// <param name="context">The context.</param>
    /// <returns>True if the version is removed; Otherwise false.</returns>
    public override bool RemoveVersion(ItemDefinition itemDefinition, VersionUri version, CallContext context)
    {
      if (!CommerceUtility.IsCommerceServerInitialized)
        return false;
      ExternalIdInformation information = (ExternalIdInformation)null;
      if (this.CanProcessCatalogItem(itemDefinition.ID, context, out information))
      {
        ISitecoreDataProvider catalogDataProvider = this.GetCatalogDataProvider(information.CommerceItemType);
        if (catalogDataProvider != null)
          return catalogDataProvider.RemoveVersion((DataProvider)this, itemDefinition, version, context, information);
      }
      return base.RemoveVersion(itemDefinition, version, context);
    }

    /// <summary>Gets the templates.</summary>
    /// <param name="context">The context.</param>
    /// <returns>
    /// The collection of Catalog templates supported by the Commerce Server data provider.
    /// </returns>
    public override TemplateCollection GetTemplates(CallContext context)
    {
      Assert.ArgumentNotNull((object)context, "context");
      this.InitializeTemplates(context.DataManager.Database);
      return base.GetTemplates(context);
    }

    /// <summary>
    /// Initializes data templates utilized by the data provider.
    /// </summary>
    /// <param name="database">The database that contains the templates.</param>
    protected override void InitializeTemplates(Database database)
    {
      Assert.ArgumentNotNull((object)database, "database");
      if (this._templatesInitializing || !CommerceUtility.IsCommerceServerInitialized || Context.IsUnitTesting || this._templatesInitialized || Context.ContentLanguage == null)
        return;
      lock (this._templateInitializationLock)
      {
        if (this._templatesInitialized)
          return;
        try
        {
          this._templatesInitializing = true;
          if (database.GetItem("/sitecore/Commerce") != null)
          {
            ICatalogTemplateGenerator instance = CommerceTypeLoader.CreateInstance<ICatalogTemplateGenerator>(typeof(ICatalogTemplateGenerator).Name, false, new object[0]);
            if (instance != null)
              instance.BuildCatalogTemplates(database);
            else
              CommerceLog.Current.Warning("ICatalogTemplateGenerator is disabled, cannot build templates", (object)this);
          }
          base.InitializeTemplates(database);
          this._templatesInitialized = true;
        }
        finally
        {
          this._templatesInitializing = false;
        }
      }
    }
  }
}
