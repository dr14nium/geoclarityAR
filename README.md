# GeoClarity

**GeoClarity** is a forward-thinking project designed to revolutionize land information services through mixed reality (MR) technologies. By integrating augmented reality (AR) and virtual reality (VR), GeoClarity dynamically renders 3D geospatial data, making it accessible and interactive for stakeholders in land management, urban planning, and property valuation.

## GeoClarityAR

**GeoClarityAR** is the AR-focused component of the GeoClarity project, built using Unity3D and powered by ARCore. This application brings geospatial data to life, allowing users to interact with 3D models derived from GeoJSON data in a real-world AR environment. GeoClarityAR is a powerful tool for visualizing land parcels, buildings, and other geospatial features.

### Key Features

- **GeoJSON Integration**: Seamlessly converts GeoJSON files into 2D and 3D models within Unity, enabling detailed geographic data visualization.
- **AR Visualization**: Anchors spatial data in the real world using ARCore, allowing users to explore and interact with 3D models in their physical environment.
- **Dynamic Model Generation**: Supports the creation of wireframe or full mesh models, with customizable attributes like color, material, and height.
- **Interactive User Interface**: A comprehensive UI system for toggling layers, adjusting visualization settings, and viewing detailed attributes for each 3D object.
- **Real-Time Location Services**: Integrates Android location services to display real-time geospatial coordinates, ensuring accurate placement of AR content.

### Core Components

- **GeospatialManager**: Oversees AR session initialization, object placement, and location services, ensuring accurate geographic anchoring and a seamless AR experience.
- **UIManager**: Manages the user interface, including controls for layers, object information panels, and visualization settings.
- **BlockFromGeoJsonBuilder**: Converts GeoJSON data into Unity objects, supporting both 2D and 3D model generation with various customization options.
- **BlockFromPolygonBuilder**: Handles the mesh generation process, including triangulation and vertex data application, for the models created by `BlockFromGeoJsonBuilder`.
- **FeatureAttributes**: Stores key-value pairs of attributes extracted from GeoJSON, enabling dynamic association with generated objects.
- **ObjectInteraction**: Facilitates user interaction with AR objects, triggering the display of attribute information via the UIManager.
- **PlaneVisualizerSetup**: Disables unnecessary AR plane visualizations to maintain focus on the geospatial content.

### How to Use GeoClarityAR

1. **Setup and Import**: Clone the GeoClarity repository and open the GeoClarityAR project in Unity. Ensure that ARCore and Google.XR.ARCoreExtensions are properly configured.
2. **Load GeoJSON Data**: Import GeoJSON files into the project and use the `BlockFromGeoJsonBuilder` script to generate 3D models.
3. **Customize Visualization**: Adjust wireframe thickness, colors, and materials in the Unity Inspector to fit your visualization needs.
4. **Run the Project**: Deploy the project to an ARCore-supported Android device. The AR experience will start automatically, displaying geospatial data anchored in the real world.
5. **Interact with AR Content**: Use the UI to toggle layers, view object details, and customize your AR experience.

### Requirements

- Unity 2020.3 or later
- ARCore SDK for Unity
- Android device with ARCore support
- GeoJSON data files for visualization

### Installation

1. **Clone the Repository**:
   `git clone https://github.com/dr14nium/geoclarityAR.git`
2. **Open the Project**: Open the cloned GeoClarityAR project in Unity.
3. **Configure ARCore**: Ensure ARCore is set up and configured correctly in Unity.
4. **Import GeoJSON Files**: Import your GeoJSON files into the project and start building your AR experience.

### Contribution

Contributions to GeoClarityAR are welcome! Please fork the repository, make your changes, and submit a pull request. For significant changes, open an issue to discuss your proposed modifications.

### References

- [GeoJSON.Net on GitHub](https://github.com/ViRGIS-Team/GeoJSON.Net)
- [GeoJsonCityBuilder on GitHub](https://github.com/ElmarJ/GeoJsonCityBuilder)
