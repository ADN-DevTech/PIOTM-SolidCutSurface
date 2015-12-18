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
using Autodesk.Civil.ApplicationServices;

// Civil 3D namespaces
using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;

#endregion

namespace ADNPlugin.Civil3D.SolidCutSurface
{
  class Util
  {
    /// <summary>
    /// Return TRUE if the current AutoCAD is
    /// Civil3D or FALSE if is not.
    /// Rely on registry key value, therefore
    /// do not use Civil3D specific library.
    /// </summary>
    public static bool IsCivil3D
    {
      get
      {
        return (HostApplicationServices.Current.
          UserRegistryProductRootKey.Contains("000"));
      }
    }

    /// <summary>
    /// Get the list of surfaces and search by name
    /// </summary>
    /// <param name="name">Name of surface to search.
    /// Case sensitive.</param>
    /// <returns>ObjectId of the surface</returns>
    public static ObjectId GetSurfaceId(string name)
    {
      Database db = Application.DocumentManager.
        MdiActiveDocument.Database;
      using (Transaction trans = db.
        TransactionManager.StartTransaction())
      {
        CivilDocument civilDoc = CivilApplication.ActiveDocument;
        ObjectIdCollection surfaceIds = civilDoc.GetSurfaceIds();
        foreach (ObjectId surfaceId in surfaceIds)
        {
          CivilSurface surface = 
            trans.GetObject(surfaceId, OpenMode.ForRead)
            as CivilSurface;
          if (surface.Name == name)
            return surface.ObjectId;
        }
      }
      return ObjectId.Null;
    }

    /// <summary>
    /// Load the bitmap from resource. The resource image 
    /// 'Build action' must be set to 'Embeded resource'.
    /// PNG extension supports transparency.
    /// </summary>
    /// <param name="imageResourceName">Resource name .PNG</param>
    /// <returns>The loaded image</returns>
    public static System.Windows.Media.ImageSource
      LoadPNGImageFromResource(string imageResourceName)
    {
      System.Reflection.Assembly dotNetAssembly =
        System.Reflection.Assembly.GetExecutingAssembly();
      System.IO.Stream iconStream =
        dotNetAssembly.GetManifestResourceStream(imageResourceName);
      System.Windows.Media.Imaging.PngBitmapDecoder bitmapDecoder =
        new System.Windows.Media.Imaging.PngBitmapDecoder(iconStream,
          System.Windows.Media.Imaging.BitmapCreateOptions.
          PreservePixelFormat, System.Windows.Media.Imaging.
          BitmapCacheOption.Default);
      System.Windows.Media.ImageSource imageSource =
        bitmapDecoder.Frames[0];
      return imageSource;
    }

    #region PInvoke to avoid 'Not Responding' status

    /// <summary>
    /// Process Windows messages to avoid 'Not Responding' state
    /// </summary>
    public static void AvoidNotResponding()
    {
      Util.NativeMessage msg;
      Util.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
    }

    [System.Runtime.InteropServices.StructLayout(
      System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativeMessage
    {
      public IntPtr handle;
      public uint msg;
      public IntPtr wParam;
      public IntPtr lParam;
      public uint time;
      public System.Drawing.Point p;
    }

    // We won't use this maliciously
    [System.Security.SuppressUnmanagedCodeSecurity] 
    [System.Runtime.InteropServices.DllImport("User32.dll",
      CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool PeekMessage(out NativeMessage msg, 
      IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, 
      uint flags);

    #endregion
  }
}
