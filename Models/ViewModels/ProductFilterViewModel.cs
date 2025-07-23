﻿using Ecom_api.Models;

public class ProductFilterViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<string> Categories { get; set; } = new();

    public string? Search { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinRating { get; set; }
    public string? Category { get; set; }
    public string? SortBy { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 6;

    public int TotalPages { get; set; }
}
