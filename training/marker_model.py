import pandas as pd # data analysis toolkit - create, read, update, delete datasets
import numpy as np #matrix math
from sklearn.model_selection import train_test_split #to split out training and testing data
#keras is a high level wrapper on top of tensorflow (machine learning library)
#The Sequential container is a linear stack of layers
from keras.models import Sequential
#popular optimization strategy that uses gradient descent
from keras.optimizers import Adam
#to save our model periodically as checkpoints for loading later
from keras.callbacks import ModelCheckpoint
#what types of layers do we want our model to have?
from keras.layers import Lambda, Activation, Conv2D, MaxPooling2D, Dropout, Dense, Flatten
#helper class to define input shape and generate training images given image paths & steering angles
from utils import INPUT_SHAPE,IMAGE_HEIGHT, IMAGE_WIDTH, IMAGE_CHANNELS, cv2_load_image
#for command line arguments
import argparse
#for reading files
import os, math

#for debugging, allows for reproducible (deterministic) results
np.random.seed(0)


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

    #yay dataframes, we can select rows and columns by their names
    #we'll store the camera images as our input data
    x_center = data_df['center']
    x_left = data_df['left']
    x_right = data_df['right']
    x = pd.concat([x_center, x_left, x_right]).values

    #and our steering commands as our output data    
    y = np.concatenate((\
        data_df[['c_x', 'c_y', 'c_width', 'c_height']].values, \
        data_df[['l_x', 'l_y', 'l_width', 'l_height']].values, \
        data_df[['r_x', 'r_y', 'r_width', 'r_height']].values))
        
    #now we can split the data into a training (80), testing(20), and validation set
    #thanks scikit learn
    x_train, x_valid, y_train, y_valid = train_test_split(x, y, test_size=args.test_size, random_state=0)

    return x_train, x_valid, y_train, y_valid


def build_model(args):
    """
    NVIDIA model used
    Image normalization to avoid saturation and make gradients work better.
    Convolution: 5x5, filter: 24, strides: 2x2, activation: ELU
    Convolution: 5x5, filter: 36, strides: 2x2, activation: ELU
    Convolution: 5x5, filter: 48, strides: 2x2, activation: ELU
    Convolution: 3x3, filter: 64, strides: 1x1, activation: ELU
    Convolution: 3x3, filter: 64, strides: 1x1, activation: ELU
    Drop out (0.5)
    Fully connected: neurons: 100, activation: ELU
    Fully connected: neurons: 50, activation: ELU
    Fully connected: neurons: 10, activation: ELU
    Fully connected: neurons: 1 (output)

    # the convolution layers are meant to handle feature engineering
    the fully connected layer for predicting the steering angle.
    dropout avoids overfitting
    ELU(Exponential linear unit) function takes care of the Vanishing gradient problem.
    """
    model = Sequential([
        Lambda(lambda x: x/127.5-1.0, input_shape=INPUT_SHAPE),
        # Flatten(),
        # Activation('relu'),
        # Dropout(0.2),
        # Dense(4)
        
        # Original Steering Model
        Conv2D(24, 5, 5, activation='elu', subsample=(2, 2)),
        Conv2D(36, 5, 5, activation='elu', subsample=(2, 2)),
        Conv2D(48, 5, 5, activation='elu', subsample=(2, 2)),
        Conv2D(64, 3, 3, activation='elu'),
        Conv2D(64, 3, 3, activation='elu'),
        Dropout(args.keep_prob),
        Flatten(),
        Dense(100, activation='elu'),
        Dense(50, activation='elu'),
        Dense(10, activation='elu'),
        Dense(4)
      ]);
      
    model.summary();

    return model


def train_model(model, args, x_train, x_valid, y_train, y_valid):
    """
    Train the model
    """
    #Saves the model after every epoch.
    #quantity to monitor, verbosity i.e logging mode (0 or 1),
    #if save_best_only is true the latest best model according to the quantity monitored will not be overwritten.
    #mode: one of {auto, min, max}. If save_best_only=True, the decision to overwrite the current save file is
    # made based on either the maximization or the minimization of the monitored quantity. For val_acc,
    #this should be max, for val_loss this should be min, etc. In auto mode, the direction is automatically
    # inferred from the name of the monitored quantity.
    checkpoint = ModelCheckpoint('model-{epoch:03d}.h5',
                                 monitor='val_loss',
                                 verbose=0,
                                 save_best_only=args.save_best_only,
                                 mode='auto')

    #calculate the difference between expected steering angle and actual steering angle
    #square the difference
    #add up all those differences for as many data points as we have
    #divide by the number of them
    #that value is our mean squared error! this is what we want to minimize via
    #gradient descent
    model.compile(loss='mean_squared_error', optimizer=Adam(lr=args.learning_rate))

    #Fits the model on data generated batch-by-batch by a Python generator.

    #The generator is run in parallel to the model, for efficiency.
    #For instance, this allows you to do real-time data augmentation on images on CPU in
    #parallel to training your model on GPU.
    #so we reshape our data into their appropriate batches and train our model simulatenously
    t_data = marking_batch_generator(args.data_dir, x_train, y_train, args.batch_size, True, args.is_unix)
    v_data = marking_batch_generator(args.data_dir, x_valid, y_valid, args.batch_size, True, args.is_unix)
    
    model.fit_generator(t_data,
                        args.samples_per_epoch,
                        args.nb_epoch,
                        max_q_size=1,
                        validation_data=v_data,
                        nb_val_samples=len(x_valid),
                        callbacks=[checkpoint],
                        verbose=1)
                       
def marking_preprocess(sourceImage, rawBounds):
    """
    Remove everything from the image that isn't in the bounding box.
    """
    # this feels backwards, but it is stored (y, x, z) not (x, y, z)
    sourceWidth = sourceImage.shape[1]
    sourceHeight = sourceImage.shape[0]
    
    # the raw bounds position represents the center of the object and is defined
    # such that (0,0) is the lower-left corner and (1,1) is the top-right. cv2 and np
    # orient (0,0) as the top-left and (1,1) as the bottom-right
    targetBounds = (
        int((rawBounds[0] * sourceWidth) - (rawBounds[2] / 2.0)),
        int(sourceHeight - (rawBounds[1] * sourceHeight) - (rawBounds[3] / 2.0)),
        int(math.ceil(rawBounds[2])),
        int(math.ceil(rawBounds[3]))
    )
    
    # create a new image with same dimensions, all white (255 all channels)
    targetImage = np.full(sourceImage.shape, 255, sourceImage.dtype)
    
    # copy just the target bounds from the source to the target, keeping in mind
    # that the first dimension is y/height and the second dimension is x/width
    targetImage[targetBounds[1]:(targetBounds[1] + targetBounds[3]), targetBounds[0]:(targetBounds[0] + targetBounds[2])] = \
        sourceImage[targetBounds[1]:(targetBounds[1] + targetBounds[3]), targetBounds[0]:(targetBounds[0] + targetBounds[2])]
        
    return targetImage
                        
def marking_batch_generator(data_dir, image_paths, bounding_rects, batch_size, is_training, is_unix):
    """
    Generate training image give image paths and associated steering angles
    """
    # batch_size = len(image_paths)
    images = np.empty([batch_size, IMAGE_HEIGHT, IMAGE_WIDTH, IMAGE_CHANNELS])
    steers = np.empty(batch_size)
    while True:
        i = 0
        for index in np.random.permutation(image_paths.shape[0]):
            if is_unix:
                image_path = flip_slashes(image_paths[index])
            else:
                image_path = image_paths[index]
            bounding_rect = bounding_rects[index]
            image = cv2_load_image(data_dir, image_path)
            images[i] = marking_preprocess(image, bounding_rect)            
            i += 1
            if i == batch_size:
                break
        yield images, bounding_rects[:batch_size]
                        
#for command line args
def s2b(s):
    """
    Converts a string to boolean value
    """
    s = s.lower()
    return s == 'true' or s == 'yes' or s == 'y' or s == '1'


def main():
    """
    Load train/validation data set and train the model
    """
    parser = argparse.ArgumentParser(description='Behavioral Cloning Training Program')
    parser.add_argument('-d', help='data directory',        dest='data_dir',          type=str,   default='data')
    parser.add_argument('-t', help='test size fraction',    dest='test_size',         type=float, default=0.2)
    parser.add_argument('-k', help='drop out probability',  dest='keep_prob',         type=float, default=0.5)
    parser.add_argument('-n', help='number of epochs',      dest='nb_epoch',          type=int,   default=10)
    parser.add_argument('-s', help='samples per epoch',     dest='samples_per_epoch', type=int,   default=20000)
    parser.add_argument('-b', help='batch size',            dest='batch_size',        type=int,   default=40)
    parser.add_argument('-o', help='save best models only', dest='save_best_only',    type=s2b,   default='true')
    parser.add_argument('-l', help='learning rate',         dest='learning_rate',     type=float, default=1.0e-4)
    parser.add_argument('-u', help='unix file name',        dest='is_unix',           type=s2b,   default='false')
    args = parser.parse_args()

    #print parameters
    print('-' * 30)
    print('Parameters')
    print('-' * 30)
    for key, value in vars(args).items():
        print('{:<20} := {}'.format(key, value))
    print('-' * 30)

    #load data
    data = load_data(args)
    #build model
    model = build_model(args)
    #train model on data, it saves as model.h5
    train_model(model, args, *data)


if __name__ == '__main__':
    main()
