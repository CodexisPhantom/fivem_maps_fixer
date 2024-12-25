using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using CodeWalker.GameFiles;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FivemMapsFixer.Models.Ymaps;

public partial class YmapsEntitiesToFix:ObservableObject
{
    private static readonly HttpClient Client = new();
    [ObservableProperty] private YmapEntityDef _baseEntity;
    [ObservableProperty] private YmapEntityDef _entity;
    [ObservableProperty] private string _ymapFullPath;
    [ObservableProperty] private string _ymapShortPath;
    [ObservableProperty] private string _entityName;
    [ObservableProperty] private bool _isToFix;
    private readonly string _hash;
    
    public YmapsEntitiesToFix(YmapEntityDef baseEntity, YmapEntityDef entity, string ymapPath, string basePath)
    {
        BaseEntity = baseEntity;
        Entity = entity;
        YmapFullPath = ymapPath;
        YmapShortPath = ymapPath.Replace(basePath, "");
        _isToFix = true;
        _entityName = entity.Name;
        _hash = entity.Name;
    }
    
    public void LoadName()
    {
        StringContent content = new StringContent("[" + _hash + "]", Encoding.UTF8, "text/plain");
        try
        {
            HttpResponseMessage response = Client.PostAsync(Settings.ApiNameUrl, content).Result;
            response.EnsureSuccessStatusCode();
            string responseBody = response.Content.ReadAsStringAsync().Result;
            responseBody = responseBody[11..];
            responseBody = responseBody.Remove(responseBody.Length - 2);
            string name = responseBody;
            if (name == "-") name = _hash;
            EntityName = name;
        }
        catch
        {
            EntityName = _hash;
        }
    }
}