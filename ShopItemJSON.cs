using Newtonsoft.Json;

[Serializable]
public class ShopItemJSON {
    [JsonProperty("id")]
    public int id {get; set;}

    [JsonProperty("title")]
    public string title {get; set;}

     [JsonProperty("description")]
    public string description {get; set;}

     [JsonProperty("category")]
    public string category {get; set;}

    [JsonProperty("rating")]
    public RateJSON rateJSON {get; set;}

    [JsonProperty("image")]
     public string image {get; set;}


}