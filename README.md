# Aircraft Machine Learning
## Introduction

This project is designed to automate the development of aircraft-type unmanned aerial vehicle simulator applications using Unity. It also provides functionality to easily train machine learning models to control these aircraft. This project - https://github.com/gasgiant/Aircraft-Physics - was taken as the basis of physics and modified.

## Features

- Realistic aerodynamics
- Realistics engines (vector of streight, rpm, power, etc)
- Wind
- Air pressure


## Demo

[![VIDEO DEMO](https://media.discordapp.net/attachments/634421986076131368/1246765695212191795/image.png?ex=665d9489&is=665c4309&hm=f5200262bcc944b70fe2596308b8a33d3d5700647e01546f8b14fa2911c00451&=&format=webp&quality=lossless&width=687&height=292)](https://drive.google.com/file/d/1cbnysOwDFN1zxDICBBvhkydX9xNlUabG/view?usp=sharing)

## Getting started

First of all, you need to create a project in Unity and add these scripts to it.

Next you need to configure the aircraft. Create an empty object and insert the following child components into it: 3D model for visualizing an airplane, colliders of airplane parts, colliders of wheels.
Next you need to create the engines. Create them as separate objects (blank or rendered) and place them in the plane object. Place them at the actual engine locations. Attach the script to the engines as a component. If you want to use basic engine physics, use the BasicEngine script, if you want to override its behavior, then use your inherited script.  
Create aerodynamic objects, place them on the plane and configure them (more detailed instructions can be found in the original physics repository). Use AeroSurface script for them.
Attach the physics script to the airplane object. Add the created engines, wheels and aerodynamic surfaces to the appropriate fields. Configure the aircraft via component fields.

## Training

For training you need to create routes. I recommend using spheres as checkpoints. And place them approximately 200 meters apart. A Checkpoint script must be attached to each checkpoint. Combine all checkpoints in the desired order under one parent route object. Attach the Route script to it.

It is necessary to add several sensors to the aircraft; I recommend using three sets of sensors. Two of them will be angled slightly up and down from the forward direction. Use a length of approximately 250 meters. Add tags of checkpoints and airfields to the objects read by the sensor.

Add the AgentController script to the aircraft, set up the training mode and attach the route. Copy the plane object several times to create more agents and speed up training

## Using models

To use trained models, replace the AgentController component with AircraftController. In the component, you need to select three trained models that will automatically switch during the process. When leaving the runway, the takeoff model switches to the flight model, and when one of the sensor beams detects the destination airfield, it switches to the landing model.
The module also allows the aircraft to make non-critical mistakes by skipping checkpoints, automatically switching the target to the next one, this allows you to avoid a long turn. Mandatory checkpoints to which the plane will turn upon passing can be marked with a special flag. The module also collects statistics about speed, overloads and checkpoints.
