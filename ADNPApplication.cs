// (C) Copyright 2011 by Autodesk, Inc. 
//
// Permission to use, copy, modify, and distribute this software
// in object code form for any purpose and without fee is hereby
// granted, provided that the above copyright notice appears in
// all copies and that both that copyright notice and the limited
// warranty and restricted rights notice below appear in all
// supporting documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK,
// INC. DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL
// BE UNINTERRUPTED OR ERROR FREE.
//
// Use, duplication, or disclosure by the U.S. Government is
// subject to restrictions set forth in FAR 52.227-19 (Commercial
// Computer Software - Restricted Rights) and DFAR 252.227-7013(c)
// (1)(ii)(Rights in Technical Data and Computer Software), as
// applicable.
//

#region Namespaces

// System namespaces
using System;

// AutoCAD namespaces
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Ribbon;

using Autodesk.Windows;

// Brep namespaces
using BrepFace = Autodesk.AutoCAD.BoundaryRepresentation.Face;
using Autodesk.AutoCAD.BoundaryRepresentation;

// Civil 3D namespaces
using Autodesk.Civil.Land.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using CivilSurface = Autodesk.Civil.Land.DatabaseServices.Surface;

#endregion

namespace ADNPlugin.Civil3D.SolidCutSurface
{
  public class ADNPApplication : IExtensionApplication
  {
    public void Initialize()
    {
      if (!Util.IsCivil3D) return;

      DemandLoading.RegistryUpdate.RegisterForDemandLoading();

      if (Autodesk.Windows.ComponentManager.Ribbon == null)
      {
        //load the custom Ribbon on startup, but at this point
        //the Ribbon control is not available, so register for
        //an event and wait
        Autodesk.Windows.ComponentManager.ItemInitialized +=
            new EventHandler<RibbonItemEventArgs>
              (ComponentManager_ItemInitialized);
      }
      else
      {
        //the assembly was loaded using NETLOAD, so the ribbon
        //is available and we just create the ribbon
        CreateRibbon();
      }
    }

    private void ComponentManager_ItemInitialized(
      object sender, RibbonItemEventArgs e)
    {
      //now one Ribbon item is initialized, but the Ribbon control
      //may not be available yet, so check if before
      if (Autodesk.Windows.ComponentManager.Ribbon != null)
      {
        //ok, create Ribbon
        CreateRibbon();
        //and remove the event handler
        Autodesk.Windows.ComponentManager.ItemInitialized -=
            new EventHandler<RibbonItemEventArgs>
              (ComponentManager_ItemInitialized);
      }
    }

    /// <summary>
    /// Creates the ribbon button for this application
    /// </summary>
    private void CreateRibbon()
    {
      RibbonControl ribCntrl =
        RibbonServices.RibbonPaletteSet.RibbonControl;
      //can also be Autodesk.Windows.ComponentManager.Ribbon;     

      foreach (RibbonTab ribTab in ribCntrl.Tabs)
      {
        if (ribTab.Title == "Add-Ins")
        {
          Autodesk.Windows.RibbonPanelSource ribSourcePanel = null;
          foreach (RibbonPanel ribPnl in ribTab.Panels)
          {
            if (ribPnl.Source.Title == "PIOTM")
              ribSourcePanel = ribPnl.Source;
          }
          if (ribSourcePanel == null)
          {
            //create the panel source
            ribSourcePanel = new RibbonPanelSource();
            ribSourcePanel.Title = "PIOTM";
            //now the panel
            RibbonPanel ribPanel = new RibbonPanel();
            ribPanel.Source = ribSourcePanel;
            ribTab.Panels.Add(ribPanel);
          }
          AddCmdToRibbonPanel(ribSourcePanel);
          return;
        }
      }
    }


    /// <summary>
    /// Add this application command button to the 
    /// specified Ribbon panel source
    /// </summary>
    /// <param name="ribSourcePanel">Ribbon panel source 
    /// to add the button</param>
    private void AddCmdToRibbonPanel(
      RibbonPanelSource ribSourcePanel)
    {
      //create button
      RibbonButton ribCmdCutSurface = new RibbonButton();
      ribCmdCutSurface.Text = "Cut Solid\non Surface";
      ribCmdCutSurface.CommandParameter = 
        ADNPCommand.CMD_CUT_SOLID_FROM_SURFACE;
      ribCmdCutSurface.ShowText = true;
      ribCmdCutSurface.ShowImage = true;
      ribCmdCutSurface.Size = RibbonItemSize.Large;
      ribCmdCutSurface.Orientation = 
        System.Windows.Controls.Orientation.Vertical;
      ribCmdCutSurface.LargeImage = 
        Util.LoadPNGImageFromResource(
        "ADNPlugin.Civil3D.SolidCutSurface.icon32.png");
      ribCmdCutSurface.CommandHandler = new AdskCommandHandler();

      //create a tooltip
      Autodesk.Windows.RibbonToolTip ribToolTip = new RibbonToolTip();
      ribToolTip.Command = "CUTSURFACE";
      ribToolTip.Title = "Cut Solid on Surface";
      ribToolTip.Content = "Generate a Civil3D TIN Surface on the " +
        "bottom of an AutoCAD solid that cut/pass through " +
        "a Civil3D TIN Surface.";
      ribToolTip.ExpandedContent = "If the name of the TIN Surface " +
        "to create already exist, this command will erase all " +
        "points of the surface and add the newly generated points." +
        "\n\nThe number of points per AutoCAD unit of drawing " +
        "represent the number of points will be added to " +
        "the new surface along the edge length, higher " +
        "values result in more dense TIN surfaces.";
      ribCmdCutSurface.ToolTip = ribToolTip;

      ribSourcePanel.Items.Add(ribCmdCutSurface);
    }

    /// <summary>
    /// Class to execute the action on ribbon buttons.
    /// Use the RibbonItem.CommandParameter as a
    /// command name.
    /// </summary>
    private class AdskCommandHandler : System.Windows.Input.ICommand
    {
      public bool CanExecute(object parameter)
      {
        return true;
      }

      public event EventHandler CanExecuteChanged;

      public void Execute(object parameter)
      {
        //is from a Ribbon Button?
        RibbonButton ribBtn = parameter as RibbonButton;
        if (ribBtn != null)
        {
          string cmdName = (String)ribBtn.CommandParameter;

          // remove all empty spaces and add
          // a new one at the end.
          cmdName = cmdName.TrimEnd() + " ";

          // execute the command using command prompt
          Application.DocumentManager.MdiActiveDocument.
            SendStringToExecute(cmdName,
            true, false, true);
        }
      }
    }

    public void Terminate() { }
  }
}
