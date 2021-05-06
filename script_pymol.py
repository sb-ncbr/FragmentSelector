# Usage: 
#     pymol -qcyr script_align.py -- mode, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds

# Imports

import sys
from pymol import cmd


# Get command line arguments

N_PSEUDOARGUMENTS = 1
arguments = sys.argv[N_PSEUDOARGUMENTS:]
if len(arguments) != 5:
    print('ERROR: Exactly 5 command line arguments required, ' + str(len(arguments)) + ' given: ' + ' '.join(arguments))
    cmd.quit(1)
mode, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds = arguments

def create_session(mode, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds):
    if mode == '0':
        create_session_without_steps(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    elif mode == '1':
        create_session_steps_mode1(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    elif mode == '2':
        create_session_steps_mode2(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    elif mode == '3':
        create_session_steps_mode3(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    elif mode == '4':
        create_session_steps_mode4(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    elif mode == '5':
        create_session_steps_mode5(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    else:
        raise ValueError('Invalid mode: ' + mode)

def create_session_without_steps(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds):
    cmd.load(inputFile, 'structure')
    cmd.load(outputFileWithoutExtension + outputFileExtension, 'result', format='pdb')
    cmd.select('central', 'id ' + centralAtomIds)
    cmd.select('res', 'result expand 0.01')

    cmd.hide()
    cmd.show('cartoon')
    cmd.show('spheres', 'central')

    cmd.color('wheat')
    cmd.color('green', 'res')
    cmd.color('blue', 'central')

    cmd.disable('all')
    cmd.enable('structure')
    cmd.zoom('res')

    cmd.delete('central')
    cmd.delete('res')
    cmd.save(outputFileWithoutExtension + '.pse')

def create_session_steps_mode1(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds):
    load_and_select_steps(5, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    cmd.color('wheat')
    cmd.color('green', 'sel1')
    cmd.color('red', 'sel1 & not sel2')
    cmd.color('marine', 'sel3 & not sel2')
    cmd.color('yellow', 'sel4 & not sel3')
    cmd.color('magenta', 'sel5 & not sel4')
    cmd.color('blue', 'central')
    show_and_save_steps(5, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)

def create_session_steps_mode2(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds):
    load_and_select_steps(5, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    cmd.color('wheat')
    cmd.color('green', 'sel1')
    cmd.color('blue', 'sel2 & not sel1')
    cmd.color('red', 'sel2 & not sel3')
    cmd.color('yellow', 'sel4 & not sel3')
    cmd.color('magenta', 'sel5 & not sel4')
    cmd.color('blue', 'central')
    show_and_save_steps(5, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)

def create_session_steps_mode3(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds):
    load_and_select_steps(5, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    cmd.color('wheat')
    cmd.color('green', 'sel1')
    cmd.color('blue', 'sel2 & not sel1')
    cmd.color('orange', 'sel3 & not sel2')
    cmd.color('red', 'sel3 & not sel4')
    cmd.color('magenta', 'sel5 & not sel4')
    cmd.color('blue', 'central')
    show_and_save_steps(5, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)

def create_session_steps_mode4(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds):
    load_and_select_steps(6, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    cmd.color('wheat')
    cmd.color('green', 'sel1')
    cmd.color('blue', 'sel2 & not sel1')
    cmd.color('red', 'sel2 & not sel3')
    cmd.color('marine', 'sel4 & not sel3')
    cmd.color('yellow', 'sel5 & not sel4')
    cmd.color('magenta', 'sel6 & not sel5')
    cmd.color('blue', 'central')
    show_and_save_steps(6, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)

def create_session_steps_mode5(inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds):
    load_and_select_steps(7, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    cmd.color('wheat')
    cmd.color('green', 'sel1')
    cmd.color('blue', 'sel2 & not sel1')
    cmd.color('orange', 'sel3 & not sel2')
    cmd.color('red', 'sel3 & not sel4')
    cmd.color('marine', 'sel5 & not sel4')
    cmd.color('yellow', 'sel6 & not sel5')
    cmd.color('magenta', 'sel7 & not sel6')
    cmd.color('blue', 'central')
    show_and_save_steps(7, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)

def load_and_select_steps(n_steps, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds):
    cmd.load(inputFile, 'structure')
    for i in range(1, n_steps+1):
        cmd.load('%s_step_%d%s' % (outputFileWithoutExtension, i, outputFileExtension), 'step%i' % i)
        cmd.select('sel%d' % i, 'step%d expand 0.01' % i)
    cmd.select('central', 'id ' + centralAtomIds)

def show_and_save_steps(n_steps, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds):
    cmd.hide()
    cmd.show('cartoon')
    cmd.show('spheres', 'central')

    cmd.disable('all')
    cmd.enable('structure')
    cmd.zoom('sel%d' % n_steps)

    cmd.delete('central')
    for i in range(1, n_steps+1):
        cmd.delete('sel%d' % i)
    cmd.save(outputFileWithoutExtension + '.pse')


# Main script
try:
    create_session(mode, inputFile, outputFileWithoutExtension, outputFileExtension, centralAtomIds)
    cmd.quit(0)
except Exception as e:
    print('ERROR: ' + type(e).__name__ + ' raised: ' + str(e))
    cmd.quit(1)
    