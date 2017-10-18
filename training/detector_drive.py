#parsing command line arguments
import argparse
#decoding camera images
import base64
#for frametimestamp saving
from datetime import datetime
#reading and writing files
import os
#high level file operations
import shutil
#matrix math
import numpy as np
#real-time server
import socketio
#concurrent networking 
import eventlet
#web server gateway interface
import eventlet.wsgi
#image manipulation
from PIL import Image
#web framework
from flask import Flask
#input output
from io import BytesIO
from datetime import datetime

#load our saved model
from keras.models import load_model

#helper class
import utils

#initialize our server
sio = socketio.Server()
#our flask (web) app
app = Flask(__name__)
#init our model and image array as empty
model = None
isTesting = False
isTracking = False
prev_image_array = None

#set min/max speed for our autonomous car
MAX_SPEED = 25
MIN_SPEED = 10

#and a speed limit
speed_limit = MAX_SPEED

#registering event handler for the server
@sio.on('telemetry')
def telemetry(sid, data):
    if data:
        # The current image from the center camera of the car
        image = Image.open(BytesIO(base64.b64decode(data["image"])))
        try:
        
            image = np.asarray(image)       # from PIL image to numpy array
            image = utils.preprocess(image) # apply the preprocessing
            image = np.array([image])       # the model expects 4D array
            
            handleTelemetry( image)
            
        except Exception as e:
            print(e)

        # save frame
        if args.image_folder != '':
            timestamp = datetime.utcnow().strftime('%Y_%m_%d_%H_%M_%S_%f')[:-3]
            image_filename = os.path.join(args.image_folder, timestamp)
            image.save('{}.jpg'.format(image_filename))
    else:        
        sio.emit('manual', data={}, skip_sid=True)
        
def handleTelemetry(image):
    if (isTracking):
        handleTelemetryTracking(image)
    else:
        handleTelemetryDetection(image)

def handleTelemetryDetection(image):
    if (isTesting):
        # Changes the 'is detecting' by toggling every 10 seconds by:
        #   [1] Get the 'seconds' part of the current time
        #   [2] Convert it to an integer
        #   [3] Convert to 1 if odd, 0 if event
        #   [4] Convert to boolean
        send_detection((bool) ((int) (datetime.now().second / 10) % 2))
    else:
        modelPrediction = model.predict(image, batch_size=1);
        print('{}: {}'.format(datetime.now(), modelPrediction))
        send_detection(bool(modelPrediction[0][0] > 0.5))

def handleTelemetryTracking(image):
    if (isTesting):
        # may want to use something better for testing purposes here
        send_track(1, 2, 3, 4)
    else:
        # predict returns a NumPy array of values, so theoretically if the model
        # is trained with the expected output sequence of x,y,width,height then this
        # will work as I'm expecting it to...
        prediction = model.predict(image, batch_size=1)
        send_track(
            double(prediction[0]), double(prediction[1]),
            double(prediction[2]), double(prediction[3]))

@sio.on('connect')
def connect(sid, environ):
    print("connect ", sid)
    if (isTracking):
        send_track(0, 0, 0, 0)
    else:
        send_detection(False)

def send_detection(isDetecting):
    print('{}'.format(isDetecting))
    sio.emit("detect",
        data={
            'hasDetection': isDetecting
        },
        skip_sid=True)

def send_track(posX, posY, sizeWidth, sizeHeight):
    print('{} {} {} {}'.format(posX, posY, sizeWidth, sizeHeight))
    sio.emit("track",
        data={
            'positionX': posX,
            'positionY': posY,
            'sizeWidth': sizeWidth,
            'sizeHeight': sizeHeight
        },
        skip_sid=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Remote Driving')
    parser.add_argument(
        'model',
        type=str,
        help='Path to model h5 file. Model should be on the same path.'
    )
    parser.add_argument(
        'image_folder',
        type=str,
        nargs='?',
        default='',
        help='Path to image folder. This is where the images from the run will be saved.'
    )
    
    parser.add_argument(
        '--track',
        action="store_true",
        default=False,
        help='Setting this flag will place the driver in "Track" mode vs "Detect" mode.'
    )
    
    parser.add_argument(
        '--test',
        action="store_true",
        default=False,
        help='Used to send test messages via the web socket instead of using the model.'
    )
    
    args = parser.parse_args()
    isTracking = args.track
    isTesting = args.test
    
    #load model
    model = load_model(args.model)

    if args.image_folder != '':
        print("Creating image folder at {}".format(args.image_folder))
        if not os.path.exists(args.image_folder):
            os.makedirs(args.image_folder)
        else:
            shutil.rmtree(args.image_folder)
            os.makedirs(args.image_folder)
        print("RECORDING THIS RUN ...")
    else:
        print("NOT RECORDING THIS RUN ...")

    # wrap Flask application with engineio's middleware
    app = socketio.Middleware(sio, app)

    # deploy as an eventlet WSGI server
    eventlet.wsgi.server(eventlet.listen(('', 4567)), app)