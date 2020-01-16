#load Settings.cake

using System;
using System.IO;

public class GAssembly
{
    private ICakeContext Cake;

    public bool Init { get; private set; }
    public string Name { get; private set; }
    public string Dir { get; private set; }
    public string GDir { get; private set; }
    public string Csproj { get; private set; }
    public string RawApi { get; private set; }
    public string Metadata { get; private set; }

    public string[] Deps { get; set; }
    public string ExtraArgs { get; set; }

    public GAssembly(string name)
    {
        Cake = Settings.Cake;
        Deps = new string[0];

        Name = name;
        Dir = Path.Combine("Source", "Libs", name);
        GDir = Path.Combine(Dir, "Generated");

        var temppath = Path.Combine(Dir, name);
        Csproj = temppath + ".csproj";
        RawApi = temppath + "-api.xml";
        Metadata = temppath + ".metadata";
    }

    public void Prepare()
    {
        Cake.CreateDirectory(GDir);
        var tempapi = Path.Combine(GDir, Name + "-api.xml");
        Cake.CopyFile(RawApi, tempapi);

        // Metadata file found, time to generate some stuff!!!
        if (Cake.FileExists(Metadata))
        {
            // Fixup API file
            var symfile = Path.Combine(Dir, Name + "-symbols.xml");
            Cake.DotNetCoreExecute("BuildOutput/Tools/GapiFixup.dll",
                "--metadata=" + Metadata + " " + "--api=" + tempapi +
                (Cake.FileExists(symfile) ? " --symbols=" + symfile : string.Empty)
            );

            var extraargs = ExtraArgs + " ";

            // Locate APIs to include
            foreach (var dep in Deps)
            {
                var ipath = Path.Combine("Source", "Libs", dep, "Generated", dep + "-api.xml");

                if (Cake.FileExists(ipath))
                    extraargs += " --include=" + ipath + " ";
            }

            // Generate code
            Cake.DotNetCoreExecute("BuildOutput/Tools/GapiCodegen.dll",
                "--outdir=" + GDir + " " +
                "--schema=Source/Libs/Shared/Gapi.xsd " +
                extraargs + " " +
                "--assembly-name=" + Name + " " +
                "--generate=" + tempapi
            );
        }

        Init = true;
    }

    public void Clean()
    {
        if (Cake.DirectoryExists(GDir))
            Cake.DeleteDirectory(GDir, new DeleteDirectorySettings { Recursive = true, Force = true });
    }
}
