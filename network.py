
#-*- coding: utf-8 -*-coding
from ctypes import pointer
import json
import os
from pickle import FALSE, TRUE
from random import shuffle
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '2'

import numpy as np
import tensorflow as tf

from tensorflow.keras.layers import Dense, GRU, Input, Dropout, Embedding, TimeDistributed, SimpleRNN, LSTM, BatchNormalization
from tensorflow.keras.models import Sequential, load_model
from tensorflow.keras.optimizers import Adam
from tensorflow.keras.preprocessing.text import Tokenizer, text_to_word_sequence
from tensorflow.keras.preprocessing.sequence import pad_sequences

print(tf.config.experimental.list_logical_devices())
print(tf.config.list_physical_devices('GPU'))
print(tf.config.experimental.list_physical_devices())

def progress_bar(now, end, old, text = ''):
    if ((30/end) * now) - ((30/end) * old) > 1:
        str = f"{now}/{end} ["
        for x in range(0, 30):
            if x < (30/end) * now:
                str += "="
                pass
            else:
                str += " "
                pass
            pass
        str += f"] {text}"
        print(str)
        pass

def shuffle_array(data, lenght):
    """
    parameters:

    data = ['input':_input, 'output': _output]
    len(_input) == len(_output)
    lenght = len(_input) or len(_output)

    return:

    {'input':_input,'output':_output}
    """
    _input  = data['input']
    _output = data['output']
    indeces = list(range(0,lenght))

    np.random.shuffle(indeces)

    _input  = np.array(_input)
    _output = np.array(_output)

    _input  = _input[indeces]
    _output = _output[indeces]
    return {'input':_input.tolist(),'output':_output.tolist()}

def cut_selection(data, lenght, size_train):
    """
    parameters:

    data = ['input':_input, 'output': _output]
    len(_input) == len(_output)
    lenght = len(_input) or len(_output)
    size_train < lenght

    return:

    {'train':{'input':_input_train,'output':_output_train},'test':{'input':_input_test,'output':_output_test},'full':data}
    """
    _input  = data['input']
    _output = data['output']
    _input_train  = []
    _output_train = []
    _input_test  = []
    _output_test = []
    for i in range(0,lenght):
        if i < size_train:
            _input_train.append(_input[i])
            _output_train.append(_output[i])
            pass
        else:
            _input_test.append(_input[i])
            _output_test.append(_output[i])
            pass
        pass
    return {'train':{'input':_input_train,'output':_output_train},'test':{'input':_input_test,'output':_output_test},'full':data}

def convert(data):
    new_data = np.full((len(data)), fill_value=0).tolist()
    for x in range(0, len(data)):
        new_data[x] = sorted(data).index(data[x]) + 1
        pass
    return(new_data)

def test(res,test):
    for x in range(0,len(res)):
            if res[x] != test[x]:
                return 0
            pass
    pass
    return 1

a = []

def quantization_points(points):
    length = len(points)
    new_points = np.full((length,2), fill_value=0)
    xy = []
    for i in range(0,length):
        if points[i][0] not in xy:
            xy.append(points[i][0])
            pass
        if points[i][1] not in xy:
            xy.append(points[i][1])
            pass
        pass
    xy.sort();
    new_points = []
    for i in range(0,length):
        new_points.append([xy.index(points[i][0]) + 1,xy.index(points[i][1]) + 1])
        pass
    return(new_points)

def normalization_points(points):
    new_points = np.full((len(points),2), fill_value=0.0)
    x = []
    y = []
    for i in range(0,len(points)):
        x.append(points[i][0])
        y.append(points[i][1])
        pass
    max_x = sorted(x)[len(x)-1]
    max_y = sorted(y)[len(y)-1]
    for i in range(0,len(points)):
        new_points[i][0] = points[i][0] / max_x
        new_points[i][1] = points[i][1] / max_y
        pass
    return(new_points)

def normalization_positions(positions):
    new_positions = np.full((len(positions)), fill_value=0.0)
    indeces = []
    for i in range(0,len(positions)):
        indeces.append(positions[i])
        pass
    max_indeces = sorted(indeces)[len(indeces)-1]
    for i in range(0,len(positions)):
        new_positions[i] = positions[i] / max_indeces
        pass
    return(new_positions)

points = np.array([
    [1.0,1.0],
    [1.0,2.0],
    [2.0,2.0],
    [2.0,1.0]
])

models = []

f = open('out_29_03_24_17', 'r')
for str_ in f.readlines():
    if str_ not in "":
        models.append(normalization_points(quantization_points(json.loads(str_)['points'])))
    pass

for i in [-0.5, 0, 0.5]:
    for j in [-0.5, 0, 0.5]:
        for i2 in [-0.5, 0, 0.5]:
            for j2 in [-0.5, 0, 0.5]:
                new_points = points.tolist()
                new_points[0] = [points[0][0] + i, points[0][1] + j]
                new_points[2] = [points[2][0] + i, points[2][1] + j]
                try:
                    models.index(normalization_points(quantization_points(new_points)))
                    pass
                except :
                    models.append(normalization_points(quantization_points(new_points)))
                    pass
                pass
            pass
        pass
    pass

print('models : ',len(models))

X = []
Y = []

for i in range(0,len(models)):
    points = models[i]
    for x in range(0,4):
        for y in range(0,4):
            for z in range(0,4):
                for w in range(0,4):
                    if ( x + y + z + w == 6 and (x + 1) * (y + 1) * (z + 1) * (w + 1) == 24):
                        X.append(points[[x,y,z,w]].tolist())
                        Y.append(normalization_positions([x + 1,y + 1,z + 1,w + 1]).tolist())
                        pass
                    pass
                pass
            pass
        pass
    pass

print('batch size : ', len(X))

batch = cut_selection(shuffle_array({'input':X,'output':Y}, len(X)),len(X), round(len(X) * 0.5))
X = batch['train']['input']
Y = batch['train']['output']

test_X = batch['test']['input']
test_Y = batch['test']['output']

model = Sequential()
model.add(LSTM(4, activation='sigmoid', input_shape = (4, 2), return_sequences = True))
model.add(BatchNormalization())
model.add(LSTM(8,  activation='sigmoid', input_shape = (4, 2), return_sequences = True))
model.add(BatchNormalization())
model.add(LSTM(8, activation='sigmoid', input_shape = (4, 2), return_sequences = True))
model.add(BatchNormalization())
model.add(LSTM(8, activation='sigmoid', input_shape = (4, 2), return_sequences = True))
model.add(BatchNormalization())
model.add(LSTM(4, activation='sigmoid', input_shape = (4, 2)))
print(model.output_shape)
model.summary()
model.compile(loss='categorical_crossentropy', metrics=['accuracy'], optimizer='Adam')
history = model.fit(X, Y, batch_size=len(X), epochs=2000, verbose=1)
model.save('model.h5')

t = 0
for x in range(0,len(test_X)):
    res = convert(model.predict([test_X[x]],verbose=0)[0])
    t += test(normalization_positions(res), test_Y[x])
    pass
print(f"res: {(100.0 / len(test_X)) * t}%")