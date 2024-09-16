import psycopg2
import json

# Fungsi untuk membuat schema dan tabel
def create_schema_and_table(conn, schema_name, table_name):
    cursor = conn.cursor()

    # Membuat schema jika belum ada
    cursor.execute(f"CREATE SCHEMA IF NOT EXISTS {schema_name};")

    # Membuat tabel dengan kolom berdasarkan GeoJSON
    create_table_query = f"""
    CREATE TABLE IF NOT EXISTS {schema_name}.{table_name} (
        id SERIAL PRIMARY KEY,
        geojson_id INTEGER,
        hgh FLOAT,
        luas FLOAT,
        lat FLOAT,
        long FLOAT,
        info TEXT,
        geom GEOMETRY(MULTIPOLYGON, 4326)
    );
    """
    cursor.execute(create_table_query)
    conn.commit()
    print(f"Table {schema_name}.{table_name} created successfully.")

# Fungsi untuk mengubah koordinat GeoJSON menjadi WKT format yang valid
def convert_multipolygon_to_wkt(coordinates):
    wkt_multipolygon = "MULTIPOLYGON("
    
    polygons = []
    for polygon in coordinates:
        rings = []
        for ring in polygon:
            points = ", ".join([f"{coord[0]} {coord[1]}" for coord in ring])
            rings.append(f"({points})")
        polygons.append(f"({', '.join(rings)})")
    
    wkt_multipolygon += f"{', '.join(polygons)})"
    
    return wkt_multipolygon

# Fungsi untuk memasukkan data dari GeoJSON ke tabel PostgreSQL
def insert_geojson_to_postgresql(conn, schema_name, table_name, geojson_data):
    cursor = conn.cursor()

    # Loop melalui fitur-fitur di dalam GeoJSON
    for feature in geojson_data['features']:
        properties = feature['properties']
        geojson_id = properties['id']
        hgh = properties['hgh']
        luas = properties['luas']
        lat = properties['LAT']
        long = properties['LONG']
        info = properties['info']

        # Konversi geometri menjadi WKT format yang valid
        coordinates = feature['geometry']['coordinates']
        multipolygon_wkt = convert_multipolygon_to_wkt(coordinates)

        # Masukkan data ke PostgreSQL
        insert_query = f"""
        INSERT INTO {schema_name}.{table_name} (geojson_id, hgh, luas, lat, long, info, geom)
        VALUES (%s, %s, %s, %s, %s, %s, ST_GeomFromText(%s, 4326));
        """
        cursor.execute(insert_query, (geojson_id, hgh, luas, lat, long, info, multipolygon_wkt))
    
    conn.commit()
    print(f"Data inserted successfully into {schema_name}.{table_name}.")

# Fungsi utama untuk menjalankan script
def main():
    # Koneksi ke PostgreSQL
    conn = psycopg2.connect(
        dbname="data",
        user="postgres",
        password="adrian$5758",
        host="localhost",
        port="8888"
    )

    # Nama schema dan tabel
    schema_name = "btadrian_schema"
    table_name = "btadrian_table"

    # Membaca file GeoJSON
    with open("D:/WORK/01_Projects/2024 001 Pix2land/Unity3D/geoclarityAR/Assets/GeoJSON/btadrian.geojson", "r") as file:
        geojson_data = json.load(file)

    # Membuat schema dan tabel
    create_schema_and_table(conn, schema_name, table_name)

    # Memasukkan data GeoJSON ke PostgreSQL
    insert_geojson_to_postgresql(conn, schema_name, table_name, geojson_data)

    # Menutup koneksi
    conn.close()

if __name__ == "__main__":
    main()
