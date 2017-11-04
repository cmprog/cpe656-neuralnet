import pandas as pd # data analysis toolkit - create, read, update, delete datasets
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
#web server gateway
import eventlet.wsgi
#image manipulation
from PIL import Image
#web framework
from flask import Flask
#input output
from io import BytesIO
from datetime import datetime
import sys
import time

#load our saved model
from keras.models import load_model

#helper class
import utils

#initialize our server
sio = socketio.Server()
#our flask (web) app
app = Flask(__name__)
#init our model and image array as empty
prev_image_array = None

#set min/max speed for our autonomous car
MAX_SPEED = 25
MIN_SPEED = 10

#and a speed limit
speed_limit = MAX_SPEED

def main():
    """
    Load train/validation data set to be evaluated
    """
    parser = argparse.ArgumentParser(description='Remote Driving')
    parser.add_argument('model', help='Path to model h5 file. Model should be on the same path.', type=str )
    parser.add_argument('-d', help='data directory', dest='data_dir', type=str, default='data')  #IMG in 'data'/IMG
    parser.add_argument('-u', help='unix file name', dest='is_unix',  type=s2b, default='false')
    parser.add_argument('--track', help='Track vs Detect.', dest='is_track',type=s2b,  default='false')
    args = parser.parse_args()


    if args.is_track:
        print("Tracking output not implemented")
        exit()

    # if args.image_folder != '':
    #     print("Creating image folder at {}".format(args.image_folder))
    #     if not os.path.exists(args.image_folder):
    #         os.makedirs(args.image_folder)
    #     else:
    #         shutil.rmtree(args.image_folder)
    #         os.makedirs(args.image_folder)
    #     print("RECORDING THIS RUN ...")
    # else:
    #     print("NOT RECORDING THIS RUN ...")

    # wrap Flask application with engineio's middleware
    #app = socketio.Middleware(sio, app)

    # deploy as an eventlet WSGI server
    #eventlet.wsgi.server(eventlet.listen(('', 4567)), app)

    #load data
    data = load_data(args)
    #load model
    model = load_model(args.model)
    #output predictions to csv
    eval_model(model, args, *data)

def load_data(args):
    """
    Load training data and split it into training and validation set
    """
    #reads CSV file into a single dataframe variable
    data_df = pd.read_csv(os.path.join(os.getcwd(), args.data_dir, \
        'driving_log.csv'), names=[\
        'center', 'c_detect', 'c_x', 'c_y', 'c_width', 'c_height', \
        'left', 'l_detect', 'l_x', 'l_y', 'l_width', 'l_height', \
        'right', 'r_detect', 'r_x', 'r_y', 'r_width', 'r_height'])

    #we'll store the camera images as our input data
    x_center = data_df['center'].values
    x_left = data_df['left'].values
    x_right = data_df['right'].values

    return x_center, x_left, x_right, data_df

def eval_model(model, args, x_center, x_left, x_right, df):
    csv_center = []
    csv_left = []
    csv_right = []
    index = 1
    total = len(x_center)
    print(" ")
    for c, l, r in zip(x_center, x_left, x_right):
        # prints and updates line instead of scrolling cmd
        restart_line()
        sys.stdout.write('Appending {} of {}'.format(index, total))
        sys.stdout.flush()

        center_prediction, left_prediction, right_prediction = append_csv(model, c, l, r)
        csv_center.append(center_prediction.item())
        csv_left.append(left_prediction.item())
        csv_right.append(right_prediction.item())
        index = index + 1
    df['center_prediction'] = csv_center
    df['left_prediction'] = csv_left
    df['right_prediction'] = csv_right
    print("Saving output file to driving_log_eval.csv")
    df.to_csv(os.path.join(os.getcwd(), args.data_dir, 'driving_log_eval.csv'), index=False)

def append_csv(model, center, left, right):
    center = utils.load_image("", center)
    center = utils.preprocess(center) # apply the preprocessing
    center = np.array([center])       # the model expects 4D array

    left = utils.load_image("", left)
    left = utils.preprocess(left) # apply the preprocessing
    left = np.array([left])       # the model expects 4D array

    right = utils.load_image("", right)
    right = utils.preprocess(right) # apply the preprocessing
    right = np.array([right])       # the model expects 4D array

    center_prediction = model.predict(center, batch_size=1)
    left_prediction = model.predict(left, batch_size=1)
    right_prediction = model.predict(right, batch_size=1)
    return center_prediction, left_prediction, right_prediction


def handleTelemetry(image):
    if (is_track):
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
            float(prediction[0][0]), float(prediction[0][1]),
            float(prediction[0][2]), float(prediction[0][3]))

@sio.on('connect')
def connect(sid, environ):
    print("connect ", sid)
    if (is_track):
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

#used for outputting to same cmd line
def restart_line():
    sys.stdout.write('  ')
    sys.stdout.flush()
    sys.stdout.write('\r')
    sys.stdout.flush()

#for command line args
def s2b(s):
    """
    Converts a string to boolean value
    """
    s = s.lower()
    return s == 'true' or s == 'yes' or s == 'y' or s == '1'

if __name__ == '__main__':
    main()
