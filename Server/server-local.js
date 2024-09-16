const express = require('express');
const { Pool } = require('pg');
const wkx = require('wkx');
const app = express();
const port = 3000;

// PostgreSQL connection configuration local server
const pool = new Pool({
  user: 'postgres',
  host: 'localhost',
  database: 'data',
  password: 'adrian$5758',
  port: 8888,
});

// Check connection to PostgreSQL
pool.connect((err, client, release) => {
  if (err) {
    console.error('Error acquiring client', err.stack);
    return;
  }
  console.log('Connected to PostgreSQL');
  release();
});

// Endpoint to fetch GeoJSON data from btadrian_table
app.get('/getBtadrianGeoJson', async (req, res) => {
  try {
    const result = await pool.query('SELECT ST_AsBinary(geom) AS geom, geojson_id, hgh, luas, lat, long, info FROM btadrian_schema.btadrian_table;');
    console.log('Query result:', result.rows);

    if (result.rows.length === 0) {
      return res.status(404).json({ error: 'No data found' });
    }

    const features = result.rows
      .map((row) => {
        try {
          const geometry = wkx.Geometry.parse(Buffer.from(row.geom, 'hex'));
          return {
            type: 'Feature',
            geometry: geometry.toGeoJSON(),
            properties: {
              geojson_id: row.geojson_id,
              hgh: row.hgh,
              luas: row.luas,
              lat: row.lat,
              long: row.long,
              info: row.info,
            },
          };
        } catch (parseError) {
          console.error('Error parsing WKB:', parseError);
          return null;
        }
      })
      .filter((feature) => feature !== null);

    if (features.length === 0) {
      return res.status(500).json({ error: 'Error processing data' });
    }

    const featureCollection = {
      type: 'FeatureCollection',
      features: features,
    };

    res.json(featureCollection);
  } catch (error) {
    console.error('Error executing query:', error.stack);
    res.status(500).json({ error: 'Internal Server Error' });
  }
});

// Start the server
app.listen(port, () => {
  console.log(`Server running on http://localhost:${port}`);
});
