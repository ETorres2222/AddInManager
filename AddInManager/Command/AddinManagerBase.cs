﻿using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAddinManager.Model;
using RevitAddinManager.ViewModel;
using System.Windows;
using static RevitAddinManager.App;

namespace RevitAddinManager.Command;

public sealed class AddinManagerBase
{
    public Result ExecuteCommand(ExternalCommandData data, ref string message, ElementSet elements, bool faceless)
    {
        if (FormControl.Instance.IsOpened) return Result.Succeeded;
        var vm = new AddInManagerViewModel(data, ref message, elements);
        if (_activeCmd != null && faceless)
        {
            return RunActiveCommand(data, ref message, elements);
        }
        FrmAddInManager = new View.FrmAddInManager(vm);
        FrmAddInManager.SetRevitAsWindowOwner();
        FrmAddInManager.SetMonitorSize();
        FrmAddInManager.Show();
        return Result.Failed;
    }

    public string ActiveTempFolder
    {
        get => _activeTempFolder;
        set => _activeTempFolder = value;
    }

    public Result RunActiveCommand(ExternalCommandData data, ref string message, ElementSet elements)
    {
        var filePath = _activeCmd.FilePath;
        if (!File.Exists(filePath))
        {
            MessageBox.Show("File not found: " + filePath,DefaultSetting.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            return 0;
        }
        Result result = Result.Failed;
        var alc = new TestAssemblyLoadContext();
        try
        {
            Trace.WriteLine("Loading assembly...");
            Assembly assembly = alc.LoadFromAssemblyPath(filePath);
            object instance = assembly.CreateInstance(_activeCmdItem.FullClassName);
            WeakReference alcWeakRef = new WeakReference(alc, trackResurrection: true);
            if (instance is IExternalCommand externalCommand)
            {
                Trace.WriteLine("Chuong");
                _activeEc = externalCommand;
                result = _activeEc.Execute(data, ref message, elements);
                alc.Unload();
            }
            int counter = 0;
            for (counter = 0; alcWeakRef.IsAlive && (counter < 10); counter++)
            {
                alc = null;
                Console.WriteLine("Waiting for unload...");
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            //TODO : Why?>???????? still can't delete inside Revit ^_^
            Console.WriteLine("Try Delete");
            File.Delete(filePath);
            if(File.Exists(filePath))
            {
                Console.WriteLine("Delete Not Done");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            MessageBox.Show(ex.ToString());
            result = Result.Failed;
        }
        finally
        {
            Trace.WriteLine("Unloading context...");
            alc.Unload();
            int counter = 0;
        }
        return result;
    }


    private void AssembUnloading(AssemblyLoadContext obj)
    {
        Console.WriteLine("Unloading.....");
    }

    public static AddinManagerBase Instance
    {
        get
        {
            if (_instance == null)
            {
#pragma warning disable RCS1059 // Avoid locking on publicly accessible instance.
                lock (typeof(AddinManagerBase))
                {
                    if (_instance == null)
                    {
                        _instance = new AddinManagerBase();
                    }
                }
#pragma warning restore RCS1059 // Avoid locking on publicly accessible instance.
            }
            return _instance;
        }
    }

    private AddinManagerBase()
    {
        _addinManager = new AddinManager();
        _activeCmd = null;
        _activeCmdItem = null;
        _activeApp = null;
        _activeAppItem = null;
    }

    public IExternalCommand ActiveEC
    {
        get => _activeEc;
        set => _activeEc = value;
    }

    public Addin ActiveCmd
    {
        get => _activeCmd;
        set => _activeCmd = value;
    }

    public AddinItem ActiveCmdItem
    {
        get => _activeCmdItem;
        set => _activeCmdItem = value;
    }

    public Addin ActiveApp
    {
        get => _activeApp;
        set => _activeApp = value;
    }

    public AddinItem ActiveAppItem
    {
        get => _activeAppItem;
        set => _activeAppItem = value;
    }

    public AddinManager AddinManager
    {
        get => _addinManager;
        set => _addinManager = value;
    }

    private string _activeTempFolder = string.Empty;

    private static volatile AddinManagerBase _instance;

    private IExternalCommand _activeEc;

    private Addin _activeCmd;

    private AddinItem _activeCmdItem;

    private Addin _activeApp;

    private AddinItem _activeAppItem;

    private AddinManager _addinManager;
}