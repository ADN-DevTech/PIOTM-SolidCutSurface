# PIOTM-SolidCutSurface

Plugin of the Month, January 2012, Brought to you by the Autodesk Developer Network

Solid Cut Surface

# Description

This plug-in generate a Civil 3D Tin Surface below an AutoCAD
solid element.

# System Requirements

This plugin has been tested with AutoCAD Civil 3D 2012
and requires the .NET Framework 4.0 

A pre-built version of the plugin has been provided which should
work on 32- and 64-bit Windows systems.

The source code has been provided as a Visual Studio 2010 project
containing C# code (not required to run the plugin).

#Installation

Copy the plugin module, "ADNPlugin-SolidCutSurface.dll", to a location
on your local system (the best place is your AutoCAD Civil 3D 2012 
application's root program folder).

Inside your Civil 3D application, use the NETLOAD command to load
the plugin. As it loads the application will register itself to load
automatically in future sessions of the Autodesk product into which
it has been loaded.

 If you are using Vista or Windows 7, first check
if the zip file needs to be unblocked.
Right-click on the zip file and select "Properties". If you see
an "Unblock" button, then click it. 

# Usage

Once loaded, run CUTSURFACE (command line or under Ribbon
Add-ins). Follow the steps:

1. Select the AutoCAD solid that will be the reference
for the surface

2. Select the Civil 3D TIN Surface to compare
the points. If no surface is selected, the new generated surface
will consider all points below the solid, if is selected, points
below the solid but above the reference surface will not be
3. Specify the new surface name. If the name already exist,
all points will be remove and replaced. Applied 
operations still exist, may require manual removal.

4. Specify the number of generated point per AutoCAD
unit, which directly affects the density and precision of
the generated surface.  High values may take longer
to process.

5. Apply one operation to simplify the surface, which
will remove unnecessary points. This is recommended
as the scan process of this command generate several
points on the same plane.

# Uninstallation

The REMOVESOLIDCUTSURFACE command can be used to 
"uninstall" the plugin, Stopping it from being loaded 
automatically in future editing sessions. 

# Limitations

AutoCAD Civil 3D do not deal well with point at the same
elevation, or drastically changes of elevation (e.g. the plane 
of the surface changing), therefore the generate surface is
less accurate on these areas. The user may manually add 
breaklines to adjust the behavior.

# Known Issues

High values of density can cause the application to hang 
due the large amount of point generated. Consider start with
small values to obtain initial surfaces.

# Author

This plugin was written by Augusto Goncalves of Autodesk Developer 
Technical Services team. 

# Acknowledgements

Autodesk Brazil Technical Specialist Daniel Queiroz who
helped with industry expertise.

# Further Reading

For more information on developing with Civil 3D, please visit the
Civil 3D Developer Center at http://www.autodesk.com/developcivil

# Feedback

Email us at labs.plugins@autodesk.com with feedback or requests for
enhancements.

# Release History

  1.0    Original release                     (January 1, 2012)

(C) Copyright 2012 by Autodesk, Inc. 

Permission to use, copy, modify, and distribute this software in
object code form for any purpose and without fee is hereby granted, 
provided that the above copyright notice appears in all copies and 
that both that copyright notice and the limited warranty and
restricted rights notice below appear in all supporting 
documentation.

AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC. 
DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
UNINTERRUPTED OR ERROR FREE.
