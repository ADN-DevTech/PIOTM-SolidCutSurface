// (C) Copyright 2012 by Autodesk, Inc. 
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

// Brep namespaces
using Autodesk.AutoCAD.BoundaryRepresentation;

// Civil 3D namespaces
using Autodesk.Civil;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;

#endregion

namespace ADNPlugin.Civil3D.SolidCutSurface
{
  public class ADNPCommand
  {
    /// <summary>
    /// This application main command name
    /// </summary>
    public const string CMD_CUT_SOLID_FROM_SURFACE = "CUTSURFACE";

    /// <summary>
    /// Pre-Command.
    /// Verify if the current application is Civil3D.
    /// </summary>
    [CommandMethod("ADNPLUGINS", CMD_CUT_SOLID_FROM_SURFACE, 
      CommandFlags.Transparent)]
    public static void CommandCutSolidFromSurface_PreVerification()
    {
      Editor ed = Application.DocumentManager.
        MdiActiveDocument.Editor;
      // Check if is Civil3D (and not AutoCAD or other vertical)
      if (!Util.IsCivil3D)
      {
        ed.WriteMessage(
          "\nThis command is available only on Civil 3D");
        return;
      }

      // Check Civil 3D version (currently only 2012)
      //if (Application.Version.Major != 18 && 
      //  Application.Version.Minor != 2)
      //{
      //  ed.WriteMessage(
      //    "\nThis command is available only on Civil 3D 2012");
      //  return;
      //}

      // Only call the command if we're sure
      // this is Civil3D. Calling this command
      // will first use AeccDbMgd reference.
      CommandCutSolidFromSurface();
    }

    /// <summary>
    /// Execute the command. Requires Civil3D.
    /// </summary>
    private static void CommandCutSolidFromSurface()
    {
      Editor ed = Application.DocumentManager.
        MdiActiveDocument.Editor;

      // select a solid
      PromptEntityOptions opSelSolid = new PromptEntityOptions(
        "\nSelect a solid: ");
      opSelSolid.SetRejectMessage("\nOnly solids allowed");
      opSelSolid.AddAllowedClass(typeof(Solid3d), true);
      PromptEntityResult resSelSolid = ed.GetEntity(opSelSolid);
      if (resSelSolid.Status != PromptStatus.OK) return;

      // select a surface
      PromptEntityOptions opSelSurface = new PromptEntityOptions(
        "\nSelect a TIN surface or : ");
      opSelSurface.SetRejectMessage("\nOnly TIN surface allowed");
      opSelSurface.AddAllowedClass(typeof(TinSurface), true);
      opSelSurface.Keywords.Add("None");
      opSelSurface.AppendKeywordsToMessage = true;
      PromptEntityResult resSelSurface = ed.GetEntity(opSelSurface);
      ObjectId surfaceId;
      switch (resSelSurface.Status)
      {
        case PromptStatus.OK:
          surfaceId = resSelSurface.ObjectId; break;
        case PromptStatus.Keyword:
          surfaceId = ObjectId.Null; break;
        default:
          return;
      }

      // specify new surface name
      PromptResult resSurfName = null;
      bool keepAskingForName = true;
      while (keepAskingForName)
      {
        PromptStringOptions opSpecifySurfName = new 
          PromptStringOptions("\nSpecify new surface name: ");
        opSpecifySurfName.AllowSpaces = true;
        opSpecifySurfName.DefaultValue = 
          "Surface<[Next Counter(CP)]>";
        opSpecifySurfName.UseDefaultValue = true;
        resSurfName = ed.GetString(opSpecifySurfName);
        if (resSurfName.Status != PromptStatus.OK) return;

        keepAskingForName = !Util.GetSurfaceId(
          resSurfName.StringResult).IsNull;
        if (keepAskingForName)
        {
          // replace points or not
          PromptKeywordOptions opReplace = new 
            PromptKeywordOptions(
            "\nSurface name already exist. Replace all points? ");
          opReplace.Keywords.Add("Yes");
          opReplace.Keywords.Add("No");
          opReplace.AppendKeywordsToMessage = true;
          PromptResult resReplace = ed.GetKeywords(opReplace);
          if (resReplace.Status != PromptStatus.OK) return;

          switch (resReplace.StringResult)
          {
            case "Yes":
              keepAskingForName = false;
              Application.ShowAlertDialog(
                "ATTENTION:\nManually remove/disable " + 
                "previous unwanted\noperations of this surface.");
              break;
            case "No":
              keepAskingForName = true;
              break;
          }
        }
      }

      // specify the density of point per unit of length
      PromptIntegerOptions opSpecifyDensity = new
        PromptIntegerOptions(
        "\nSpecify number of surface points per " +
        "AutoCAD unit of drawing: ");
      opSpecifyDensity.AllowZero = false;
      opSpecifyDensity.AllowNegative = false;
      opSpecifyDensity.DefaultValue = 5;
      opSpecifyDensity.UseDefaultValue = true;
      PromptIntegerResult resDensity = ed.GetInteger(
        opSpecifyDensity);
      if (resDensity.Status != PromptStatus.OK) return;

      // simplify surface (using Civil 3D built-in operation)
      PromptKeywordOptions opSimplify = new PromptKeywordOptions(
      "\nSimplify points? (Recommended, operation can be undone): ");
      opSimplify.Keywords.Add("Yes");
      opSimplify.Keywords.Add("No");
      opSimplify.AppendKeywordsToMessage = true;
      PromptResult resSimplify = ed.GetKeywords(opSimplify);
      if (resSimplify.Status != PromptStatus.OK) return;

      GenerateSurfaceByScan(resSelSolid.ObjectId, 
        surfaceId, resSurfName.StringResult, 
        resDensity.Value, (resSimplify.StringResult=="Yes"));
    }

    #region Scan Solid methods

    // Objects used on various functions
    private static Solid3d _solid = null;
    private static TinSurface _surface = null;
    private static Brep _brepSolid = null;
    private static Point3dCollection _newPoints = null;

    // Database and transaction objects
    private static Database _db;
    private static Transaction _trans;

    // Elevation value used when no TIN surface is selected
    private static double _upperLimit;
    
    /// <summary>
    /// Scan the solid from bottom, identify points and create 
    /// a surface
    /// </summary>
    /// <param name="solidId">Solid to scan</param>
    /// <param name="tinSurfaceToCutId">Referecen surface</param>
    /// <param name="solidSurfaceName">Name of the new 
    /// surface that will be created</param>
    /// <param name="densityOfPoints">Number of points per 
    /// AutoCAD unit used on scan</param>
    /// <param name="simplifySurface">Whether o not simplify 
    /// the surface at the end (using Civil 3D 
    /// built-in operation)</param>
    private static void GenerateSurfaceByScan(
      ObjectId solidId,
      ObjectId tinSurfaceToCutId, 
      string solidSurfaceName, 
      int densityOfPoints,
      bool simplifySurface)
    {
      _db = Application.DocumentManager.MdiActiveDocument.Database;
      using (_trans = _db.TransactionManager.StartTransaction())
      {
        try
        {
          // open entities
          _solid = _trans.GetObject(solidId, OpenMode.ForRead) 
            as Solid3d;
          if (!tinSurfaceToCutId.IsNull) _surface = 
            _trans.GetObject(tinSurfaceToCutId, OpenMode.ForRead) 
            as TinSurface;

          // extract the Brep of the solid
          _brepSolid = new Brep(_solid);
          _newPoints = new Point3dCollection();

          // get the extend of the solid
          Extents3d extends = _solid.GeometricExtents;
          // and expand by 20% to increase accuracy
          // on the solid edges/borders
          extends.TransformBy(Matrix3d.Scaling(1.2,
            _solid.MassProperties.Centroid));

          // geometric line at the bottom (virtual datum)
          // this line is the scan line
          //
          //  x--------------------------x 
          //  |  pt2        ^            pt3
          //  |             direction 
          //  |             of scan progress
          //  |
          //  | <-scan line
          //  |
          //  x  pt1
          //
          Point3d scanLinePt1 = extends.MinPoint;
          Point3d scanLinePt2 = new Point3d(
            scanLinePt1.X, extends.MaxPoint.Y, scanLinePt1.Z);
          LineSegment3d scanLine = new LineSegment3d(
            scanLinePt1, scanLinePt2);
          Point3d scanLinePt3 = new Point3d(
            extends.MaxPoint.X, extends.MaxPoint.Y, scanLinePt1.Z);
          _upperLimit = extends.MaxPoint.Z; // scan upper limit

          int numberOfScanLines = ((int)
            Math.Round(scanLinePt2.DistanceTo(scanLinePt3), 
            MidpointRounding.ToEven)) * densityOfPoints;

          ProgressMeter progressBar = new ProgressMeter();
          progressBar.SetLimit(numberOfScanLines);
          progressBar.Start("Scanning solid...");

          for (int i = 0; i < numberOfScanLines; i++)
          {
            ProcessScanLine(scanLine, densityOfPoints, true);

            // move the scan line over the scane direction
            // get direction vector
            Vector3d scanLineDisplacementDirection =
              scanLinePt2.GetVectorTo(scanLinePt3);
            // make unit vector
            scanLineDisplacementDirection /= 
              scanLineDisplacementDirection.Length;
            // adjust size
            scanLineDisplacementDirection *= 
              (1.0 / densityOfPoints);
            scanLine.TransformBy(Matrix3d.Displacement(
              scanLineDisplacementDirection));

            progressBar.MeterProgress();
            Util.AvoidNotResponding();
          }
          progressBar.Stop();
          scanLine.Dispose();

          #region For testing only
#if DEBUG
          BlockTableRecord mSpace = _trans.GetObject(_db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
          foreach (Point3d pt in _newPoints)
          {
            DBPoint p = new DBPoint(pt);
            mSpace.AppendEntity(p);
            _trans.AddNewlyCreatedDBObject(p, true);
          }
#endif
          #endregion

          CreateSolidSurface(solidSurfaceName, simplifySurface);

          _trans.Commit();
        }
        catch (System.Exception ex)
        {
          Application.DocumentManager.MdiActiveDocument.
            Editor.WriteMessage("Error: Operation aborted ({0})",
            ex.Message);
          _trans.Abort();
        }
        finally
        {
          // final cleanup
          if (!_brepSolid.IsDisposed) _brepSolid.Dispose();
          if (!_newPoints.IsDisposed) _newPoints.Dispose();
          _solid = null;
          _surface = null;
          _brepSolid = null;
          _newPoints = null;
        }
      }
    }

    /// <summary>
    /// Create the suface
    /// </summary>
    /// <param name="solidSurfaceName">Name of the surface</param>
    /// <param name="simplifySurface">Apply simplify 
    /// operation</param>
    private static void CreateSolidSurface(
      string solidSurfaceName, bool simplifySurface)
    {
      // open or create the new surface
      TinSurface newSurface = null;
      ObjectId newSurfaceId = Util.GetSurfaceId(solidSurfaceName);
      if (!newSurfaceId.IsNull)
      {
        // open, remove all points and operations
        newSurface = _trans.GetObject(newSurfaceId, 
          OpenMode.ForWrite) as TinSurface;
        newSurface.DeleteVertices(newSurface.Vertices);
      }
      else
      {
        // create and open
        newSurfaceId = TinSurface.Create(_db, solidSurfaceName);
        newSurface = _trans.GetObject(newSurfaceId, 
          OpenMode.ForWrite) as TinSurface;
      }

      // add the newly created points
      newSurface.AddVertices(_newPoints);

      // simplify surface
      if (simplifySurface)
      {
        SurfaceSimplifyOptions simplifyOptions = new 
          SurfaceSimplifyOptions(SurfaceSimplifyType.PointRemoval);
        simplifyOptions.MaximumChangeInElevation = 0.0001;
        simplifyOptions.UseMaximumChangeInElevation = true;
        newSurface.SimplifySurface(simplifyOptions);
      }
    }

    /// <summary>
    /// Process point below over the scanLine
    /// </summary>
    /// <param name="scanLine">Eeference line used to generate 
    /// points</param>
    /// <param name="densityOfPoints">Number of points 
    /// per AutoCAD unit of drawing</param>
    /// <param name="increaseOnStep">Reprocess when a step 
    /// is identified. Used to control recursion</param>
    private static void ProcessScanLine(LineSegment3d scanLine, 
      int densityOfPoints, bool increaseOnStep)
    {
      PointOnCurve3d[] pointsOnDatum = 
        scanLine.GetSamplePoints(
        (int)(scanLine.Length * densityOfPoints));
      Point3d lastPointAdded = Point3d.Origin;
      foreach (PointOnCurve3d pointOnDatumCurve in pointsOnDatum)
      {
        Point3d pointOnDatum = new Point3d(
          pointOnDatumCurve.Point.ToArray());
        double elevationOnSurface = 
          (_surface != null ?
          _surface.FindElevationAtXY(pointOnDatum.X, pointOnDatum.Y) : // up to elevation of the reference surface
          _upperLimit); // up to the limit
        Point3d pointOnSurface = new Point3d(
          pointOnDatum.X, pointOnDatum.Y, elevationOnSurface);

        // check if this point is below the solid
        // by how many hits a vertical line from
        // this point hit the solid. The lower
        // Z value is the one.
        LinearEntity3d lineEnt = new LineSegment3d(
          pointOnDatum, pointOnSurface);
        Hit[] hits = _brepSolid.GetLineContainment(lineEnt, 1);
        if (hits != null)
        {
          double lowerZ = hits[0].Point.Z; //double.MaxValue;
          //foreach (Hit h in hits)
          //{
          //  if (h.Point.Z < lowerZ)
          //    lowerZ = h.Point.Z;
          //  h.Dispose();
          //}
          Point3d pointToAdd = new Point3d(
            pointOnDatum.X, pointOnDatum.Y, lowerZ);
          _newPoints.Add(pointToAdd);

          // increase density on steps (big change on elevation)?
          // ToDo: Need to implement that on the other direction.
          //       This is working only over the scan line direction,
          //       but is also required on the perpendicular
          if (increaseOnStep)
          {
            //skip very first point
            if (lastPointAdded.DistanceTo(Point3d.Origin) != 0)
            {
              if (pointToAdd.DistanceTo(lastPointAdded) >
                ((1.0 / densityOfPoints) * 10))
              {
                double scanLineElevation = scanLine.StartPoint.Z;
                Point3d scanLinePt1 = new Point3d(
                  lastPointAdded.X, lastPointAdded.Y, 
                  scanLineElevation);
                Point3d scanLinePt2 = new Point3d(
                  pointToAdd.X, pointToAdd.Y, scanLineElevation);
                LineSegment3d increaseScanLine = new LineSegment3d(
                  scanLinePt1, scanLinePt2);
                ProcessScanLine(increaseScanLine, 
                  densityOfPoints * 10, false);
                increaseScanLine.Dispose();
              }
            }
            lastPointAdded = pointToAdd;
          }
        }
        pointOnDatumCurve.Dispose();
      }
    }

    #endregion

  }
}
