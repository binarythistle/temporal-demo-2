using marketplacer.Dtos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("SqlLiteConnection")));


var app = builder.Build();

app.MapPost("/api/sellers", async (CreateSellerDto dto, AppDbContext db) =>
{
    var seller = new Seller
    {
        SellerName = dto.SellerName,
        SellerDomain = dto.SellerDomain,
        HubSpotId = dto.HubSpotId
    };

    db.Sellers.Add(seller);
    await db.SaveChangesAsync();

    return Results.Created($"/api/sellers/{seller.Id}", new SellerDto(
        seller.Id,
        seller.SellerName,
        seller.SellerDomain,
        seller.HubSpotId
    ));
});

app.MapGet("/api/sellers/{id}", async (int id, AppDbContext db) =>
{
    var seller = await db.Sellers.FindAsync(id);

    return seller is null
        ? Results.NotFound()
        : Results.Ok(new SellerDto(
            seller.Id,
            seller.SellerName,
            seller.SellerDomain,
            seller.HubSpotId
        ));
});

app.MapGet("/api/sellers", async (AppDbContext db) =>
{
    var sellers = await db.Sellers
        .Select(s => new SellerDto(
            s.Id,
            s.SellerName,
            s.SellerDomain,
            s.HubSpotId
        ))
        .ToListAsync();

    return Results.Ok(sellers);
});

app.MapPut("/api/sellers/{id}", async (int id, UpdateSellerDto dto, AppDbContext db) =>
{
    
    var seller = await db.Sellers.FindAsync(id);

    if (seller is null)
    {
        return Results.NotFound();
    }

    if (dto.SellerName is not null)
    {
        seller.SellerName = dto.SellerName;
    }
    if (dto.SellerDomain is not null)
    {
        seller.SellerDomain = dto.SellerDomain;
    }
    if (dto.HubSpotId is not null)
    {
        seller.HubSpotId = dto.HubSpotId;
    }

    await db.SaveChangesAsync();

    return Results.Ok(new SellerDto(
        seller.Id,
        seller.SellerName,
        seller.SellerDomain,
        seller.HubSpotId
    ));
});

app.MapDelete("/api/sellers/{id}", async (int id, AppDbContext db) =>
{
    var seller = await db.Sellers.FindAsync(id);

    if (seller is null)
    {
        return Results.NotFound();
    }

    db.Sellers.Remove(seller);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.Run();

