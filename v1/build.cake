/********** ARGUMENTS **********/

var target = Argument("target", "Default");
var root = Argument("root", "../../");
var cake = Argument("recipe", "recipe.yml");

/********** TOOLS & ADDINS **********/

#addin "Cake.FileHelpers"

#addin "Cake.Yaml"

/********** TYPES **********/

public class Env {
  public const string CakeBuildVersion = "CAKE_BUILD_VERSION";
}

public class CakeYmlBuild {
  public string dist { get;set; }
  public string release_notes { get;set; }
}

public class CakeYmlComponent {
  public string name { get;set; }
  public string path { get;set; }
  public CakeYmlComponentBuild build { get;set; }
}

public class CakeYmlComponentBuild {
  public string type { get;set; }
  public string dist { get;set; }
}

public class CakeYmlBundler {
  public string name { get;set; }
  public List<CakeYmlBundlerCommand> commands { get;set; }
}

public class CakeYmlBundlerCommand {
  public string operation { get;set; }
  public CakeYmlBundlerCommandTarget from { get;set; }
  public CakeYmlBundlerCommandTarget to { get;set; }
}

public class CakeYmlBundlerCommandTarget {
  public string component { get;set; }
  public string context { get;set; }
  public string path { get;set; }
  public string extensions { get;set; }
}

public class CakeYmlArtifact {
  public string name { get;set; }
  public string path { get;set; }
  public List<CakeYmlArtifactBundler> bundler { get;set; }
}

public class CakeYmlArtifactBundler {
  public string name { get;set; }
  public bool enable_compression { get;set; }
}

public class CakeYml {
    public int version { get;set; }
    public string name { get;set; }
    public Dictionary<string,string> environment { get; set; }
    public CakeYmlBuild build { get; set; }
    public List<CakeYmlComponent> components { get;set; }
    public List<CakeYmlBundler> bundlers { get;set; }
    public List<CakeYmlArtifact> artifacts { get;set; }
}

/********** FUNCTIONS **********/

Func<CakeYml> cakeYml = () => { return DeserializeYamlFromFile<CakeYml>(root + cake); };

Action cakeYmlValidateScript = () => {
  var ymlVersion = cakeYml().version;
  if (ymlVersion != version) {
    throw new Exception(String.Format("The recipe version is not supported (current supported version is {0}).", version));
  }
};

Func<String> cakeYmlGetReleaseNotes = () => {
  var file = "ReleaseNotes.md" ;
  if (cakeYml().build != null)
  {
    var buildFile = cakeYml().build.release_notes;
    file = String.IsNullOrWhiteSpace(buildFile) ? file :buildFile;
  }
  return root + file;
};

Func<string[]> cakeYmlLoadEnvironment = () => {
  if(cakeYml().environment == null) {
    return new String[0];
  }
  foreach(var item in cakeYml().environment) {
    Environment.SetEnvironmentVariable(item.Key, item.Value);
  }
  return cakeYml().environment.Keys.ToArray();
};

Action<String> BuildComponents = (String npmCommand) => {
  foreach (var component in cakeYml().components)
  {
    Information("component: " + component.name);
    if ((component.build.type ?? "").ToLower() == "npm") {
      Information("running " + npmCommand);
      StartProcess("cmd", new ProcessSettings {
        Arguments = "/c \""+ npmCommand +"\"",
        WorkingDirectory = MakeAbsolute(Directory(root + component.path))
      });
    }
    else {
      Information("invalid build type");
    }
  }
};

/********** GLOBAL VARIABLES **********/

readonly int version = 1;
readonly bool isAPPVEYOR = (EnvironmentVariable("APPVEYOR") ?? "").ToUpper() == "TRUE";

var buildNumber = Argument<string>("build_number", null);
buildNumber = string.IsNullOrWhiteSpace(buildNumber) == false ? buildNumber : isAPPVEYOR ? EnvironmentVariable("APPVEYOR_BUILD_NUMBER") : EnvironmentVariable(Env.CakeBuildVersion);
var buildVersion = isAPPVEYOR ? EnvironmentVariable("APPVEYOR_BUILD_VERSION") : ParseReleaseNotes(cakeYmlGetReleaseNotes()).Version.ToString() + "." + buildNumber;

/********** SETUP / TEARDOWN **********/

Setup(context =>
{
    //Executed BEFORE the first task.
    cakeYmlValidateScript();
    Information("[SETUP] Build Version {0} of {1}", buildVersion, cakeYml().name);
    Information("[WORKING_DIRECTORY] {0}", MakeAbsolute(Directory(".")));
    Information("[ROOT_DIRECTORY] {0}", MakeAbsolute(Directory(root)));
    var envKeys = cakeYmlLoadEnvironment();
    Information("[ENVIRONMENT]");
    foreach(var envKey in envKeys) {
      Information("- {0}={1}", envKey, EnvironmentVariable(envKey));
    }
});

Teardown(context =>
{
    // Executed AFTER the last task.
    Information("[Teardown] Build Version {0} of {1}", buildVersion, cakeYml().name);
});

/********** TASK TARGETS **********/

Task("Clean")
  .Does(() =>
  {
    BuildComponents("npm run clean");
  });

Task("Setup")
  .Does(() =>
  {
    BuildComponents("npm install");
  });

Task("Build")
  .Does(() =>
  {
    BuildComponents("npm run build");
  });

Task("Test")
  .Does(() =>
  {
    BuildComponents("npm run test");
  });

Task("Package")
  .Does(() =>
  {
    /*foreach (var artifact in cakeYml().Artifacts)
    {
      Information("artifact: " + artifact.Name);
      if ((artifact.BundleType ?? "").ToLower() == "cmd") {
        var workingDirectory =  MakeAbsolute(Directory("."));
        Information("working directory: " + workingDirectory);
        var script = "bundle-" + artifact.Name + ".cmd";
        Information("running script: " + script);
        StartProcess("cmd", new ProcessSettings {
          Arguments = "/c \""+ script +"\"",
          WorkingDirectory = workingDirectory
        });
      }
      else {
        Information("invalid bundle type");
      }
    }*/
  });

Task("CI")
  .IsDependentOn("Clean")
  .IsDependentOn("Setup")
  .IsDependentOn("Build")
  .IsDependentOn("Test")
  .Does(() =>
  {
      Information("CI target completed");
  });

Task("RC")
  .IsDependentOn("Clean")
  .IsDependentOn("Setup")
  .IsDependentOn("Build")
  //.IsDependentOn("Test")
  .IsDependentOn("Package")
  .Does(() =>
  {
      Information("RC target completed");
  });

Task("Default")
    .IsDependentOn("RC");

RunTarget(target);
