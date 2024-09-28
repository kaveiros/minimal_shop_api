using System.ComponentModel.DataAnnotations;

public class User {
    public int Id{get; set;}

    public string Email{get; set;} = String.Empty;

    public string Passwd{get; set;} = String.Empty;

    public string Role{get; set;} = "customer";

}
