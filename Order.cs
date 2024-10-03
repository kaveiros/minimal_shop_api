public class Order {
    public int Id {get; set;}
    public int UserId {get; set;}

    public string OrderDate {get; set;}

    public List<ShopItem> ShopItems {get; set;}

}