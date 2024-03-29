#CorticonVocabularyBuilder

Console application that generates Corticon Vocabulary files from .NET models.

###View the demo on YouTube
Watch quick a demonstration on YouTube (:33): https://youtu.be/1Dqf3n2iwIg

###
Configuration:

You must set the paths to CORTICON_HOME and CORTICON_WORK_DIR in the App.config

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5" />
    </startup>
    <appSettings>
      <add key="CORTICON_HOME" value="C:\Program Files\Progress\Corticon 5.5\Server .NET\webservice" />
      <add key="CORTICON_WORK_DIR" value="C:\Program Files\Progress\Corticon 5.5\Server .NET\webservice" />
    </appSettings>
</configuration>
```

###
Usage:

To include all namespaces in the assembly:

```shell
cvbuilder generate -f <pathtoyourdll> -o <directorytosaveoutput>
```

To include a selection of namespaces in the assembly:

```shell
cvbuilder generate -f <pathtoyourdll> -o <directorytosaveoutput> -n <somenamespace> <somenamespace>
```

_An example model is included as the SampleModel project._