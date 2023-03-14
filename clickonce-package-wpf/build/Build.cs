using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "src" / "*.Tests";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath PublishDirectory => RootDirectory / "publish";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            // EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s =>
            s.SetProjectFile(Solution)
            );
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
              .SetProjectFile(Solution)
              .SetConfiguration(Configuration)
              .EnableNoRestore());
        });

    Target Publish => _ => _
    .DependsOn(Compile)
    .Executes(() =>
       {
           var publishConfigurations =
               from project in Solution.GetProjects("*Client")
               from framework in project.GetTargetFrameworks()
               select new { project, framework };

           DotNetPublish(_ => _
               .SetConfiguration(Configuration)
               .CombineWith(publishConfigurations, (_, v) => _
                   .SetProject(v.project)
                   .SetFramework(v.framework)));
       });

    Target ClickOnce => _ => _
    .DependsOn(Restore)
    .Executes(() =>
    {
        var csprojFile = RootDirectory + @"\code\code.csproj";
        var publishProfile = "ClickOnceProfile";

        MSBuildTasks.MSBuild(s => s
            .SetTargetPath(csprojFile)
            .SetTargets("Clean", "Restore", "Build", "Publish")
            .SetConfiguration(Configuration)
            .SetNodeReuse(IsLocalBuild)
            .SetProperty("DeployOnBuild", "true")
            .SetProperty("PublishProfile", publishProfile)
            .SetProperty("Platform", "AnyCPU")
            .SetProperty("PublishDir", PublishDirectory)
            );

        // Key for this to work was to include the following line in the code project:
        // <RuntimeIdentifier>win-x86</RuntimeIdentifier>
    });
}